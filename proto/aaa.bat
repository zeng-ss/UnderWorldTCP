
@echo off
 
set "file_path=%cd%\out"
 
IF NOT EXIST %file_path% (
    mkdir %file_path%
) 


for %%i in (proto/*.proto) do (
    echo %%i
	%cd%/bin/protoc -I=proto --csharp_out=out %%i
)

pause