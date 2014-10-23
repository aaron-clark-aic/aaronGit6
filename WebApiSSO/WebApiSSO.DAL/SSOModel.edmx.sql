
-- --------------------------------------------------
-- Entity Designer DDL Script for SQL Server 2005, 2008, 2012 and Azure
-- --------------------------------------------------
-- Date Created: 10/23/2014 10:01:14
-- Generated from EDMX file: F:\document\project\自我完善\GitTest\aaronGit6\WebApiSSO\WebApiSSO.DAL\SSOModel.edmx
-- --------------------------------------------------

SET QUOTED_IDENTIFIER OFF;
GO
USE [Test9527];
GO
IF SCHEMA_ID(N'dbo') IS NULL EXECUTE(N'CREATE SCHEMA [dbo]');
GO

-- --------------------------------------------------
-- Dropping existing FOREIGN KEY constraints
-- --------------------------------------------------

IF OBJECT_ID(N'[dbo].[FK_User_Token_Base_User]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[User_Token] DROP CONSTRAINT [FK_User_Token_Base_User];
GO

-- --------------------------------------------------
-- Dropping existing tables
-- --------------------------------------------------

IF OBJECT_ID(N'[dbo].[Base_User]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Base_User];
GO
IF OBJECT_ID(N'[dbo].[User_Token]', 'U') IS NOT NULL
    DROP TABLE [dbo].[User_Token];
GO

-- --------------------------------------------------
-- Creating all tables
-- --------------------------------------------------

-- Creating table 'UserToken'
CREATE TABLE [dbo].[UserToken] (
    [Token] uniqueidentifier  NOT NULL,
    [LastTime] datetime  NOT NULL,
    [UserId] int  NOT NULL,
    [ClientId] smallint  NOT NULL,
    [ClientName] nvarchar(256)  NULL,
    [Enabled] bit  NOT NULL
);
GO

-- Creating table 'User'
CREATE TABLE [dbo].[User] (
    [User_ID] int IDENTITY(1,1) NOT NULL,
    [User_Name] varchar(50)  NOT NULL,
    [User_Passwd] varchar(50)  NOT NULL,
    [User_Reg_time] datetime  NULL
);
GO

-- --------------------------------------------------
-- Creating all PRIMARY KEY constraints
-- --------------------------------------------------

-- Creating primary key on [Token] in table 'UserToken'
ALTER TABLE [dbo].[UserToken]
ADD CONSTRAINT [PK_UserToken]
    PRIMARY KEY CLUSTERED ([Token] ASC);
GO

-- Creating primary key on [User_ID] in table 'User'
ALTER TABLE [dbo].[User]
ADD CONSTRAINT [PK_User]
    PRIMARY KEY CLUSTERED ([User_ID] ASC);
GO

-- --------------------------------------------------
-- Creating all FOREIGN KEY constraints
-- --------------------------------------------------

-- Creating foreign key on [UserId] in table 'UserToken'
ALTER TABLE [dbo].[UserToken]
ADD CONSTRAINT [FK_User_Token_Base_User]
    FOREIGN KEY ([UserId])
    REFERENCES [dbo].[User]
        ([User_ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_User_Token_Base_User'
CREATE INDEX [IX_FK_User_Token_Base_User]
ON [dbo].[UserToken]
    ([UserId]);
GO

-- --------------------------------------------------
-- Script has ended
-- --------------------------------------------------