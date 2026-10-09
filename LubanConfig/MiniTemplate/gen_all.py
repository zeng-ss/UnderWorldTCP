# -*- coding: utf-8 -*-
"""
Luban 一键生成：服务端 + 客户端。

用法:
    cd LubanConfig/MiniTemplate
    python gen_all.py

做四件事:
  1. 复用 gen_server.py 的逻辑，生成服务端 (Server/Network/LubanManager)
  2. 生成客户端目标 (cs-bin + bin)
  3. 客户端代码 → Assets/Scripts/HotUpdate/Luban/Cfg/，运行时 → .../Luban/LubanLib/
  4. 客户端数据 → Assets/Res/Luban/*.bytes，并自动登记进 Addressables 的 Config 组

说明:
  - vector2/3/4 是 builtin.xml 的占位 bean，两端都不需要，自动过滤。
  - .meta 文件按「路径哈希」生成确定性 GUID；已存在的 .meta 一律保留原 GUID。
  - 重新生成会删除 Cfg/ 下多余的历史文件（表被删掉时不留残骸）。
"""

import hashlib
import os
import re
import shutil
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))                 # .../LubanConfig/MiniTemplate
REPO_ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))

LUBAN_EXE = os.path.join(HERE, "Luban", "Luban.exe")
LUBAN_CONF = os.path.join(HERE, "luban.conf")

# 客户端落点
CLIENT_LUBAN_DIR = os.path.join(REPO_ROOT, "Assets", "Scripts", "HotUpdate", "Luban")
CLIENT_CFG_DIR = os.path.join(CLIENT_LUBAN_DIR, "Cfg")
CLIENT_RUNTIME_DIR = os.path.join(CLIENT_LUBAN_DIR, "LubanLib")
CLIENT_DATA_DIR = os.path.join(REPO_ROOT, "Assets", "Res", "Luban")

# 服务端 Luban 运行时所在（作为客户端运行时的拷贝源）
SERVER_LUBAN_LIB = os.path.join(REPO_ROOT, "Server", "Network", "LubanManager", "LubanLib")

ADDRESSABLES_GROUP = os.path.join(
    REPO_ROOT, "Assets", "AddressableAssetsData", "AssetGroups", "Config.asset")

TMP_DIR = os.path.join(REPO_ROOT, "_luban_client_tmp")

SKIP_CODE_PREFIXES = ("vector",)


# --------------------------------------------------------------- 基础工具
def run(cmd, cwd):
    print(">> " + " ".join(cmd))
    r = subprocess.run(cmd, cwd=cwd, capture_output=True, text=True,
                       encoding="utf-8", errors="replace")
    if r.returncode != 0:
        print(r.stdout)
        print(r.stderr)
        raise SystemExit("Luban 执行失败")
    for line in r.stdout.splitlines():
        if "ERROR" in line or "run failed" in line:
            raise SystemExit("Luban 报错：" + line)
    return r.stdout


def guid_for(path):
    """按相对仓库根路径生成确定性 GUID（32 位小写 hex）。"""
    rel = os.path.relpath(path, REPO_ROOT).replace("\\", "/").lower()
    return hashlib.md5(("UnderWordYoo/" + rel).encode("utf-8")).hexdigest()


def ensure_folder_meta(folder):
    """确保目录有 .meta（Unity 需要，否则每次导入都会重新分配 GUID）。"""
    meta = folder + ".meta"
    if os.path.exists(meta):
        return
    with open(meta, "w", encoding="utf-8", newline="\n") as f:
        f.write("fileFormatVersion: 2\n"
                "guid: %s\n"
                "folderAsset: yes\n"
                "DefaultImporter:\n"
                "  externalObjects: {}\n"
                "  userData: \n"
                "  assetBundleName: \n"
                "  assetBundleVariant: \n" % guid_for(folder))
    print("  [meta] 新建目录 meta", os.path.relpath(meta, REPO_ROOT))


def ensure_file_meta(path):
    """确保文件有 .meta（.cs 用 MonoImporter，其余用 TextScriptImporter）。"""
    meta = path + ".meta"
    if os.path.exists(meta):
        return
    guid = guid_for(path)
    if path.endswith(".cs"):
        body = ("fileFormatVersion: 2\n"
                "guid: %s\n"
                "MonoImporter:\n"
                "  externalObjects: {}\n"
                "  serializedVersion: 2\n"
                "  defaultReferences: []\n"
                "  executionOrder: 0\n"
                "  icon: {instanceID: 0}\n"
                "  userData: \n"
                "  assetBundleName: \n"
                "  assetBundleVariant: \n" % guid)
    else:
        body = ("fileFormatVersion: 2\n"
                "guid: %s\n"
                "TextScriptImporter:\n"
                "  externalObjects: {}\n"
                "  userData: \n"
                "  assetBundleName: \n"
                "  assetBundleVariant: \n" % guid)
    with open(meta, "w", encoding="utf-8", newline="\n") as f:
        f.write(body)


def sync_dir(src, dst, skip_prefixes=(), exts=(".cs",)):
    """把 src 下符合条件的文件同步到 dst（先清掉 dst 里同名类型的旧文件）。"""
    os.makedirs(dst, exist_ok=True)
    wanted = set()
    for fn in sorted(os.listdir(src)):
        if not fn.endswith(exts):
            continue
        if fn.startswith(skip_prefixes):
            print("  [跳过]", fn)
            continue
        wanted.add(fn)

    # 删除 dst 里已不再生成的文件（连同 .meta）
    for fn in os.listdir(dst):
        if fn.endswith(".meta"):
            continue
        if fn not in wanted:
            print("  [删除过期]", fn)
            os.remove(os.path.join(dst, fn))
            m = os.path.join(dst, fn + ".meta")
            if os.path.exists(m):
                os.remove(m)

    for fn in sorted(wanted):
        shutil.copyfile(os.path.join(src, fn), os.path.join(dst, fn))
        ensure_file_meta(os.path.join(dst, fn))
        print("  [拷贝]", fn)


# --------------------------------------------------------------- Addressables
def register_addressables(byte_files):
    """把 luban 的 .bytes 登记进 Addressables 的 Config 组（幂等），地址 = 文件名去后缀。"""
    if not os.path.exists(ADDRESSABLES_GROUP):
        print("  !! 找不到 Addressables Config 组，跳过登记。请手动标记这些资源：")
        for f in byte_files:
            print("     ", os.path.relpath(f, REPO_ROOT))
        return

    with open(ADDRESSABLES_GROUP, "r", encoding="utf-8") as f:
        text = f.read()

    added = []
    for path in byte_files:
        guid = guid_for(path)
        if guid in text:
            continue
        addr = os.path.splitext(os.path.basename(path))[0]
        entry = ("  - m_GUID: %s\n"
                 "    m_Address: %s\n"
                 "    m_ReadOnly: 0\n"
                 "    m_SerializedLabels: []\n"
                 "    FlaggedDuringContentUpdateRestriction: 0\n" % (guid, addr))
        anchor = "  m_SerializeEntries:\n"
        idx = text.index(anchor) + len(anchor)
        text = text[:idx] + entry + text[idx:]
        added.append(addr)

    if added:
        with open(ADDRESSABLES_GROUP, "w", encoding="utf-8", newline="\n") as f:
            f.write(text)
        print("  [Addressables] 新增登记:", ", ".join(added))
    else:
        print("  [Addressables] 已登记，无需改动")


# --------------------------------------------------------------- 主流程
def gen_server():
    print("== 1. 生成服务端 ==")
    sys.path.insert(0, HERE)
    import gen_server
    gen_server.main()


def gen_client():
    print("== 2. 生成客户端目标 ==")
    if os.path.exists(TMP_DIR):
        shutil.rmtree(TMP_DIR)
    code_dir = os.path.join(TMP_DIR, "code")
    data_dir = os.path.join(TMP_DIR, "data")
    os.makedirs(code_dir)
    os.makedirs(data_dir)

    run([LUBAN_EXE, "-t", "client", "-c", "cs-bin", "-d", "bin",
         "--conf", LUBAN_CONF,
         "-x", "outputCodeDir=" + code_dir,
         "-x", "outputDataDir=" + data_dir], cwd=HERE)

    print("== 3. 落盘客户端代码 ==")
    for d in (CLIENT_LUBAN_DIR, CLIENT_CFG_DIR, CLIENT_RUNTIME_DIR, CLIENT_DATA_DIR):
        os.makedirs(d, exist_ok=True)
        ensure_folder_meta(d)
    # Luban 运行时：从服务端那份拷过来（两端必须一致）
    for fn in ("BeanBase.cs", "ByteBuf.cs", "ITypeId.cs", "StringUtil.cs"):
        shutil.copyfile(os.path.join(SERVER_LUBAN_LIB, fn),
                        os.path.join(CLIENT_RUNTIME_DIR, fn))
        ensure_file_meta(os.path.join(CLIENT_RUNTIME_DIR, fn))
        print("  [拷贝运行时]", fn)
    sync_dir(code_dir, CLIENT_CFG_DIR, skip_prefixes=SKIP_CODE_PREFIXES)

    print("== 4. 落盘客户端数据 ==")
    byte_files = []
    wanted = {fn for fn in os.listdir(data_dir) if fn.endswith(".bytes")}
    for fn in os.listdir(CLIENT_DATA_DIR):
        if fn.endswith(".bytes") and fn not in wanted:
            print("  [删除过期]", fn)
            os.remove(os.path.join(CLIENT_DATA_DIR, fn))
            if os.path.exists(os.path.join(CLIENT_DATA_DIR, fn + ".meta")):
                os.remove(os.path.join(CLIENT_DATA_DIR, fn + ".meta"))
    for fn in sorted(wanted):
        dst = os.path.join(CLIENT_DATA_DIR, fn)
        shutil.copyfile(os.path.join(data_dir, fn), dst)
        ensure_file_meta(dst)
        byte_files.append(dst)
        print("  [拷贝]", fn)

    print("== 5. 登记 Addressables ==")
    register_addressables(byte_files)

    shutil.rmtree(TMP_DIR)


if __name__ == "__main__":
    gen_server()
    gen_client()
    print("== 全部完成。回到 Unity 等编译，首次需要重新打一次 Addressables 资源包 ==")
