--
-- Creates the DotNetNuke user and assigns the default permissions.
--
-- SQLCMD -U %dbAdminUsr%@%dbServer% -P %dbAdminPwd% -S tcp:%dbServer%.database.windows.net -d %dbName% -i -N CreateUser.sql
--
PRINT ('Creating User:  $(dbUsr)')
GO
CREATE USER $(dbUsr) FROM LOGIN $(dbUsr)
GO

EXEC sp_addrolemember 'db_owner', '$(dbUsr)'
GO
	
PRINT ('Adding User Role:  db_ddladmin')
EXEC SP_ADDROLEMEMBER 'db_ddladmin', '$(dbUsr)'
GO

PRINT ('Adding User Role:  db_securityadmin')
EXEC SP_ADDROLEMEMBER 'db_securityadmin', '$(dbUsr)'
GO
	
PRINT ('Adding User Role:  db_datareader')
EXEC SP_ADDROLEMEMBER 'db_datareader', '$(dbUsr)'
GO

PRINT ('Adding User Role:  db_datawriter')
EXEC SP_ADDROLEMEMBER 'db_datawriter', '$(dbUsr)'
GO

PRINT('Principals')
SELECT Name FROM sys.database_principals 
GO
EXIT