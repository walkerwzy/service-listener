@echo off
::���÷�������
set service_name=test
::���÷�������
set service_description=test
::���÷������·��
set prog_path=F:\\DeskTop\\yuanqi\\My DBank\\ServiceListener\\ServiceListener\\bin\\Debug\\ServiceListener.exe
::���÷����������ʽ auto:�Զ� demand:�ֶ� disabled:����
set strt=auto
::������Դ�ļ�·��
set srcpath=F:\DeskTop\yuanqi\My DBank\ServiceListener\ServiceWrapper\bin\Debug\src


echo                         ��ʼת��
echo =========================================================== 

::====���²����������޸�==================
set s32=%windir%\system32
set reg_file=ServiceInstall.reg

net stop %service_name% 2>nul
::���ĵ���ԴĿ¼�����������ϵͳ�ļ�
cd/d %srcpath%
if not exist %s32%\instsrv.exe (copy instsrv.exe %s32%)
if not exist %s32%\srvany.exe (copy srvany.exe %s32%)

%s32%\instsrv.exe %service_name% remove 2>nul
%s32%\instsrv.exe %service_name% %s32%\srvany.exe >nul

echo ���ɷ���ɹ�������ע��Ӧ�ó���...

::���÷����������ʽ auto:�Զ� demand:�ֶ� disabled:����
sc config %service_name% start= %strt%
sc description %service_name% "%service_description%"

echo ����ע����ļ�...
echo Windows Registry Editor Version 5.00 > %reg_file%
echo [HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\%service_name%\Parameters] >> %reg_file%
echo "Application"="%prog_path%" >> %reg_file%

echo ����ע����ļ�...
%reg_file%
net start %service_name%
del %reg_file%
echo ===========================���============================
pause