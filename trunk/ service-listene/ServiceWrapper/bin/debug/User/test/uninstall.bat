@echo off
set service_name=test
set reg_file=UninstallService.reg
echo ��ʼж��;
sc stop %service_name%
sc delete %service_name%
echo ���
pause
