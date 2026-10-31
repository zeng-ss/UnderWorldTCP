import zipfile, shutil, os

src = r"D:/ASUS/桌面/面试答题口述稿.docx"
tmp = r"D:/Unity/UnderWordYoo/output/90325633-c9d4-416a-856f-b95e3112cfc7/working/patched.docx"

z = zipfile.ZipFile(src)
xml = z.read("word/document.xml").decode("utf-8")

# 1) 底纹卡片表格改为满版宽度（auto -> 100%）
n_tbl = xml.count('<w:tblW w:w="0" w:type="auto"/>')
xml = xml.replace('<w:tblW w:w="0" w:type="auto"/>', '<w:tblW w:w="5000" w:type="pct"/>')

# 2) 目录域只收 Heading2（章节），避免 F9 更新后把文档标题也收进去
old_toc = ' TOC \\o "1-1" \\h \\z \\u '
new_toc = ' TOC \\o "2-2" \\h \\z \\u '
n_toc = xml.count(old_toc)
xml = xml.replace(old_toc, new_toc)

# 3) 补页面尺寸与页边距（A4，左右 2.5cm，上 2.5cm，下 2cm）
old_sect = '<w:type w:val="nextPage"/></w:sectPr>'
new_sect = ('<w:type w:val="nextPage"/>'
            '<w:pgSz w:w="11906" w:h="16838"/>'
            '<w:pgMar w:top="1418" w:right="1418" w:bottom="1134" w:left="1418" '
            'w:header="851" w:footer="992" w:gutter="0"/>'
            '</w:sectPr>')
n_sect = xml.count(old_sect)
xml = xml.replace(old_sect, new_sect)

items = [(i, z.read(i.filename)) for i in z.infolist()]
z.close()

with zipfile.ZipFile(tmp, "w", zipfile.ZIP_DEFLATED) as out:
    for info, data in items:
        if info.filename == "word/document.xml":
            data = xml.encode("utf-8")
        out.writestr(info, data)

shutil.move(tmp, src)
print("tblW patched:", n_tbl, "| toc patched:", n_toc, "| sect patched:", n_sect)
print("size:", os.path.getsize(src))
