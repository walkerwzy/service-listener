@echo off
set service_name=test
set reg_file=UninstallService.reg
echo 开始卸载;
sc stop %service_name%
sc delete %service_name%
echo 完成
pause
