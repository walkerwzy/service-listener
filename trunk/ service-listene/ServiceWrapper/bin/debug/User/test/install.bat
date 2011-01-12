@echo off
::设置服务名称
set service_name=test
::设置服务描述
set service_description=test
::设置服务程序路径
set prog_path=F:\\DeskTop\\yuanqi\\My DBank\\ServiceListener\\ServiceListener\\bin\\Debug\\ServiceListener.exe
::设置服务的启动方式 auto:自动 demand:手动 disabled:禁用
set strt=auto
::设置资源文件路径
set srcpath=F:\DeskTop\yuanqi\My DBank\ServiceListener\ServiceWrapper\bin\Debug\src


echo                         开始转换
echo =========================================================== 

::====以下部分勿随意修改==================
set s32=%windir%\system32
set reg_file=ServiceInstall.reg

net stop %service_name% 2>nul
::更改到资源目录，拷贝必需的系统文件
cd/d %srcpath%
if not exist %s32%\instsrv.exe (copy instsrv.exe %s32%)
if not exist %s32%\srvany.exe (copy srvany.exe %s32%)

%s32%\instsrv.exe %service_name% remove 2>nul
%s32%\instsrv.exe %service_name% %s32%\srvany.exe >nul

echo 生成服务成功，正在注册应用程序...

::设置服务的启动方式 auto:自动 demand:手动 disabled:禁用
sc config %service_name% start= %strt%
sc description %service_name% "%service_description%"

echo 生成注册表文件...
echo Windows Registry Editor Version 5.00 > %reg_file%
echo [HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\%service_name%\Parameters] >> %reg_file%
echo "Application"="%prog_path%" >> %reg_file%

echo 导入注册表文件...
%reg_file%
net start %service_name%
del %reg_file%
echo ===========================完成============================
pause