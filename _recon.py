# -*- coding: utf-8 -*-
import os, re, collections

ROOT = r"D:/Unity/UnderWordYoo/Assets/Scripts/HotUpdate"

type_decl = re.compile(r'^\s*(?:public|internal|abstract|sealed|static|partial|\s)*\b(?:class|interface|enum|struct)\s+(\w+)', re.M)

files = []
for dp, dn, fn in os.walk(ROOT):
    for f in fn:
        if f.endswith(".cs"):
            files.append(os.path.join(dp, f))
files.sort()

# namespace -> types ; type -> list of namespaces
ns_types = collections.defaultdict(set)
type_ns = collections.defaultdict(set)
for p in files:
    rel = os.path.relpath(p, os.path.dirname(ROOT)).replace("\\", "/")
    parts = rel.split("/")[:-1]  # drop filename
    ns = ".".join(parts)
    txt = open(p, encoding="utf-8").read()
    for m in type_decl.finditer(txt):
        t = m.group(1)
        ns_types[ns].add(t)
        type_ns[t].add(ns)

print("=== 命名空间清单 ===")
for ns in sorted(ns_types):
    print(f"{ns}: {len(ns_types[ns])} types")

print("\n=== 重复类型名(跨命名空间) ===")
dup = {t: sorted(s) for t, s in type_ns.items() if len(s) > 1}
if dup:
    for t, s in dup.items():
        print(f"  {t}: {s}")
else:
    print("  (无)")

print("\n=== verbatim 字符串 (@\") 出现的文件 ===")
for p in files:
    txt = open(p, encoding="utf-8").read()
    if '@"' in txt:
        n = txt.count('@"')
        print(f"  {os.path.relpath(p, ROOT)}: {n}")

print("\n=== 含 #if / #region 等预处理 ===")
for p in files:
    txt = open(p, encoding="utf-8").read()
    tags = re.findall(r'^\s*#\w+', txt, re.M)
    tags = sorted(set(tags))
    if any(t not in ("#region", "#endregion") for t in tags):
        print(f"  {os.path.relpath(p, ROOT)}: {tags}")

print("\n=== 文件头结构(前几条非空行) ===")
for p in files[:6]:
    print("---", os.path.relpath(p, ROOT))
    for line in open(p, encoding="utf-8").read().splitlines()[:12]:
        print("   |", line)
