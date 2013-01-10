--
-- Creates the DotNetNuke database and default user login.
--
-- SQLCMD -U %dbAdminUsr%@%dbServer% -P %dbAdminPwd% -S tcp:%dbServer%.database.windows.net -d master -i -N CreateDatabase.sql
--
--PRINT 'Creating Login:  $(dbUsr)';
--GO
--CREATE LOGIN $(dbUsr) WITH PASSWORD = '$(dbPwd)';
--GO
--PRINT 'Creating Database:  $(dbName)';
--GO
--CREATE DATABASE $(dbName);
--GO
--PRINT('---------------------------')
IF EXISTS( SELECT Name FROM sys.databases AS DATABASES WHERE Name='$(dbName)')
	PRINT('Database Verified: $(dbName)')
ELSE 
	PRINT('Database Not Found: $(dbName)')

IF EXISTS( SELECT Name FROM sys.sql_logins AS LOGINS WHERE Name='$(dbUsr)')
	PRINT('Login Verified: $(dbUsr)')
ELSE 
	PRINT('Login Account Not Found: $(dbUsr)')
	
--SELECT ' ' + Name FROM sys.databases
--PRINT('LOGINS')
--SELECT ' ' + Name FROM sys.sql_logins
--PRINT('PRINCIPALS')
--SELECT ' ' + Name FROM sys.database_principals 
--PRINT('---------------------------')
GO
EXIT