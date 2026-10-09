# -*- coding: utf-8 -*-
"""校验迁移结果：括号平衡、命名空间唯一、using 覆盖检查"""
import os, re, sys
sys.path.insert(0, r"D:/Unity/UnderWordYoo")
import importlib.util
spec = importlib.util.spec_from_file_location("mig", r"D:/Unity/UnderWordYoo/_migrate_ns.py")
mig = importlib.util.module_from_spec(spec)
spec.loader.exec_module(mig)

bad = []
for p in mig.all_files():
    rel = os.path.relpath(p, mig.ROOT).replace("\\", "/")
    text, _, _ = mig.read(p)
    code = mig.strip_code(text)
    ob, cb = code.count("{"), code.count("}")
    ns = len(re.findall(r'(?m)^namespace\s', text))
    if ob != cb:
        bad.append((rel, f"braces {ob} vs {cb}"))
    if ns != 1:
        bad.append((rel, f"namespace count {ns}"))

print("=== 结构校验 ===")
if bad:
    for rel, msg in bad:
        print(f"  !! {rel}: {msg}")
else:
    print("  所有文件：括号平衡 + 命名空间唯一 OK")

# using 覆盖检查（基于迁移后的实际文件）
type_ns = {}
for p in mig.all_files():
    n = mig.compute_ns(p)
    c = mig.strip_code(mig.read(p)[0])
    for m in mig.DECL_RE.finditer(c):
        type_ns.setdefault(m.group(1), set()).add(n)
    for m in mig.DELEGATE_RE.finditer(c):
        type_ns.setdefault(m.group(1), set()).add(n)

missing = []
for p in mig.all_files():
    rel = os.path.relpath(p, mig.ROOT).replace("\\", "/")
    n = mig.compute_ns(p)
    text, _, _ = mig.read(p)
    code = mig.strip_code(text)
    usings = set(l.strip()[6:-1] for l in text.split("\n") if mig.USING_RE.match(l.strip()))
    idents = set(mig.IDENT_RE.findall(code))
    for t in idents:
        for tns in type_ns.get(t, ()):
            if tns != n and tns not in usings:
                missing.append((rel, t, tns))

print("\n=== using 覆盖校验（漏加=编译错误） ===")
if missing:
    for rel, t, tns in missing:
        print(f"  !! {rel}: 用到 {t} (在 {tns}) 但缺 using")
else:
    print("  全部覆盖 OK")

# 每个文件的 using 是否被真正用到（多加了=Rider警告）
print("\n=== 多余 using 提示（仅统计，不影响编译） ===")
extra_cnt = 0
for p in mig.all_files():
    rel = os.path.relpath(p, mig.ROOT).replace("\\", "/")
    n = mig.compute_ns(p)
    text, _, _ = mig.read(p)
    code = mig.strip_code(text)
    idents = set(mig.IDENT_RE.findall(code))
    usings = [l.strip()[6:-1] for l in text.split("\n") if mig.USING_RE.match(l.strip())]
    for u in usings:
        if not u.startswith("HotUpdate"):
            continue
        if u == n:
            continue
        used = any(u in type_ns.get(t, set()) for t in idents)
        if not used:
            extra_cnt += 1
            print(f"  ~ {rel}: using {u} 疑似未使用")
print(f"  疑似多余 {extra_cnt} 条")
