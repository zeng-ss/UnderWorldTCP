# -*- coding: utf-8 -*-
"""
把 HotUpdate 目录下所有 .cs 补上 HotUpdate.<子目录> 命名空间（Rider 目录约定），
并自动补齐跨命名空间所需的 using。
用法: python _migrate_ns.py [apply]
"""
import os, re, sys

ROOT = r"D:/Unity/UnderWordYoo/Assets/Scripts/HotUpdate"
APPLY = len(sys.argv) > 1 and sys.argv[1] == "apply"

USING_RE = re.compile(r'^using\s+[A-Za-z_][\w\.]*\s*(=\s*[A-Za-z_][\w\.]*\s*)?;$')
NS_RE = re.compile(r'^namespace\s')
DECL_RE = re.compile(
    r'^\s*(?:\[[^\]]*\]\s*)?(?:public|internal|abstract|sealed|static|partial|\s)*'
    r'\b(?:class|interface|enum|struct)\s+(\w+)', re.M)
DELEGATE_RE = re.compile(r'\bdelegate\s+[^;{]*?\b(\w+)\s*\(')
IDENT_RE = re.compile(r'\b[A-Za-z_]\w*\b')


def strip_code(text):
    """单遍扫描，剥离 // 行注释、/* */ 块注释、字符串与字符字面量。"""
    out = []
    i, n = 0, len(text)
    while i < n:
        c = text[i]
        if c == '/' and i + 1 < n:
            if text[i + 1] == '/':
                j = text.find('\n', i)
                if j == -1:
                    break
                i = j
                continue
            if text[i + 1] == '*':
                j = text.find('*/', i + 2)
                i = (j + 2) if j != -1 else n
                continue
        if c == '"' or c == "'":
            q = c
            i += 1
            while i < n:
                if text[i] == '\\':
                    i += 2
                    continue
                if text[i] == q:
                    i += 1
                    break
                i += 1
            out.append('""')
            continue
        out.append(c)
        i += 1
    return ''.join(out)


def compute_ns(path):
    rel = os.path.relpath(path, ROOT).replace("\\", "/")
    parts = rel.split("/")[:-1]
    return "HotUpdate." + ".".join(parts) if parts else "HotUpdate"


def all_files():
    out = []
    for dp, dn, fn in os.walk(ROOT):
        for f in fn:
            if f.endswith(".cs"):
                out.append(os.path.join(dp, f))
    return sorted(out)


def read(path):
    b = open(path, "rb").read()
    bom = b.startswith(b"\xef\xbb\xbf")
    text = b.decode("utf-8-sig")
    crlf = "\r\n" in text
    return text.replace("\r\n", "\n"), bom, crlf


def sort_usings(lst):
    seen, res = set(), []
    for u in lst:
        u = u.strip()
        if u in seen or not u:
            continue
        seen.add(u)
        res.append(u)

    def key(u):
        body = u[len("using "):-1].strip()
        alias = "=" in body
        target = body.split("=")[-1].strip() if alias else body
        sysp = 0 if (target == "System" or target.startswith("System.")) else 1
        return (1 if alias else 0, sysp, target.lower())

    return sorted(res, key=key)


# ---- 1. 建类型 -> 命名空间 映射（基于剥离注释后的代码） ----
files = all_files()
type_ns = {}
for p in files:
    ns = compute_ns(p)
    code = strip_code(read(p)[0])
    for m in DECL_RE.finditer(code):
        type_ns.setdefault(m.group(1), set()).add(ns)
    for m in DELEGATE_RE.finditer(code):
        type_ns.setdefault(m.group(1), set()).add(ns)

report = []
for p in files:
    rel = os.path.relpath(p, ROOT).replace("\\", "/")
    ns = compute_ns(p)
    text, bom, crlf = read(p)
    lines = text.split("\n")
    already = any(NS_RE.match(l) for l in lines)

    code = strip_code(text)
    idents = set(IDENT_RE.findall(code))
    needed_ns = set()
    for t in idents:
        for tns in type_ns.get(t, ()):
            if tns != ns:
                needed_ns.add(tns)
    needed_usings = sorted("using %s;" % n for n in needed_ns)

    if already:
        nidx = next(i for i, l in enumerate(lines) if NS_RE.match(l))
        existing = set(l.strip() for l in lines[:nidx] if USING_RE.match(l.strip()))
        add = [u for u in needed_usings if u not in existing]
        if not add:
            continue
        last_using = -1
        for i in range(nidx):
            if USING_RE.match(lines[i].strip()):
                last_using = i
        at = last_using + 1 if last_using >= 0 else 0
        lines[at:at] = add
        out = "\n".join(lines)
        report.append((rel, ns, "已有ns,补using", add))
    else:
        usings = [l.strip() for l in lines if USING_RE.match(l.strip())]
        body = [l for l in lines if not USING_RE.match(l.strip())]
        while body and body[0].strip() == "":
            body.pop(0)
        while body and body[-1].strip() == "":
            body.pop()
        allu = sort_usings(usings + needed_usings)
        indented = [("    " + l if l.strip() else "") for l in body]
        head = ("\n".join(allu) + "\n\n") if allu else ""
        out = head + "namespace %s\n{\n" % ns + "\n".join(indented) + "\n}\n"
        report.append((rel, ns, "包namespace", list(needed_usings)))

    if crlf:
        out = out.replace("\n", "\r\n")
    if APPLY:
        data = (b"\xef\xbb\xbf" if bom else b"") + out.encode("utf-8")
        open(p, "wb").write(data)

print("=== 已应用 ===" if APPLY else "=== 干跑预览 ===")
total_usings = 0
for rel, ns, act, add in report:
    total_usings += len(add)
    print(f"[{act}] {rel}  -> {ns}" + (("   +" + ", ".join(a[6:-1] for a in add)) if add else ""))
print(f"\n共处理 {len(report)} 个文件，总 {len(files)} 个，补 using {total_usings} 条")
