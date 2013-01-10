--
-- Creates the DotNetNuke database and default user login.
--
-- SQLCMD -U %dbAdminUsr%@%dbServer% -P %dbAdminPwd% -S tcp:%dbServer%.database.windows.net -d master -i -N CreateDatabase.sql
--
PRINT 'Creating Login:  $(dbUsr)';
GO
CREATE LOGIN $(dbUsr) WITH PASSWORD = '$(dbPwd)';
GO
PRINT 'Creating Database:  $(dbName)';
GO
CREATE DATABASE $(dbName);
GO
PRINT('---------------------------')
PRINT('DATABASES')
SELECT ' ' + Name FROM sys.databases
PRINT('LOGINS')
SELECT ' ' + Name FROM sys.sql_logins
PRINT('PRINCIPALES')
SELECT ' ' + Name FROM sys.database_principals 
PRINT('---------------------------')
GO
EXIT