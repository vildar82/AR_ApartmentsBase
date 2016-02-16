
-- --------------------------------------------------
-- Entity Designer DDL Script for SQL Server 2005, 2008, 2012 and Azure
-- --------------------------------------------------
-- Date Created: 02/16/2016 15:17:36
-- Generated from EDMX file: C:\dev\АР\AR_ApartmentsBase\AR_ApartmentsBase\Model\DB\EntityModel\Model.edmx
-- --------------------------------------------------

SET QUOTED_IDENTIFIER OFF;
GO
USE [SAPR];
GO
IF SCHEMA_ID(N'dbo') IS NULL EXECUTE(N'CREATE SCHEMA [dbo]');
GO

-- --------------------------------------------------
-- Dropping existing FOREIGN KEY constraints
-- --------------------------------------------------

IF OBJECT_ID(N'[dbo].[FK_F_nn_Category_Parameters_F_S_Categories]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[F_nn_Category_Parameters] DROP CONSTRAINT [FK_F_nn_Category_Parameters_F_S_Categories];
GO
IF OBJECT_ID(N'[dbo].[FK_F_nn_Category_Parameters_F_S_Parameters]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[F_nn_Category_Parameters] DROP CONSTRAINT [FK_F_nn_Category_Parameters_F_S_Parameters];
GO
IF OBJECT_ID(N'[dbo].[FK_F_nn_ElementParam_Value_F_nn_Category_Parameters]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[F_nn_ElementParam_Value] DROP CONSTRAINT [FK_F_nn_ElementParam_Value_F_nn_Category_Parameters];
GO
IF OBJECT_ID(N'[dbo].[FK_F_nn_ElementParam_Value_F_nn_Elements_FlatModules]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[F_nn_ElementParam_Value] DROP CONSTRAINT [FK_F_nn_ElementParam_Value_F_nn_Elements_FlatModules];
GO
IF OBJECT_ID(N'[dbo].[FK_F_nn_Elements_FlatModules_F_R_FlatModules]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[F_nn_Elements_FlatModules] DROP CONSTRAINT [FK_F_nn_Elements_FlatModules_F_R_FlatModules];
GO
IF OBJECT_ID(N'[dbo].[FK_F_nn_Elements_FlatModules_F_S_Elements]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[F_nn_Elements_FlatModules] DROP CONSTRAINT [FK_F_nn_Elements_FlatModules_F_S_Elements];
GO
IF OBJECT_ID(N'[dbo].[FK_F_R_FlatModules_F_R_Flats1]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[F_nn_FlatModules] DROP CONSTRAINT [FK_F_R_FlatModules_F_R_Flats1];
GO
IF OBJECT_ID(N'[dbo].[FK_F_R_FlatModules_F_R_Modules]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[F_nn_FlatModules] DROP CONSTRAINT [FK_F_R_FlatModules_F_R_Modules];
GO
IF OBJECT_ID(N'[dbo].[FK_F_S_Elements_F_S_Categories]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[F_S_Elements] DROP CONSTRAINT [FK_F_S_Elements_F_S_Categories];
GO
IF OBJECT_ID(N'[dbo].[FK_F_S_Elements_F_S_FamilyInfos]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[F_S_Elements] DROP CONSTRAINT [FK_F_S_Elements_F_S_FamilyInfos];
GO

-- --------------------------------------------------
-- Dropping existing tables
-- --------------------------------------------------

IF OBJECT_ID(N'[dbo].[F_nn_Category_Parameters]', 'U') IS NOT NULL
    DROP TABLE [dbo].[F_nn_Category_Parameters];
GO
IF OBJECT_ID(N'[dbo].[F_nn_ElementParam_Value]', 'U') IS NOT NULL
    DROP TABLE [dbo].[F_nn_ElementParam_Value];
GO
IF OBJECT_ID(N'[dbo].[F_nn_Elements_FlatModules]', 'U') IS NOT NULL
    DROP TABLE [dbo].[F_nn_Elements_FlatModules];
GO
IF OBJECT_ID(N'[dbo].[F_nn_FlatModules]', 'U') IS NOT NULL
    DROP TABLE [dbo].[F_nn_FlatModules];
GO
IF OBJECT_ID(N'[dbo].[F_R_Flats]', 'U') IS NOT NULL
    DROP TABLE [dbo].[F_R_Flats];
GO
IF OBJECT_ID(N'[dbo].[F_R_Modules]', 'U') IS NOT NULL
    DROP TABLE [dbo].[F_R_Modules];
GO
IF OBJECT_ID(N'[dbo].[F_S_Categories]', 'U') IS NOT NULL
    DROP TABLE [dbo].[F_S_Categories];
GO
IF OBJECT_ID(N'[dbo].[F_S_Elements]', 'U') IS NOT NULL
    DROP TABLE [dbo].[F_S_Elements];
GO
IF OBJECT_ID(N'[dbo].[F_S_FamilyInfos]', 'U') IS NOT NULL
    DROP TABLE [dbo].[F_S_FamilyInfos];
GO
IF OBJECT_ID(N'[dbo].[F_S_Parameters]', 'U') IS NOT NULL
    DROP TABLE [dbo].[F_S_Parameters];
GO

-- --------------------------------------------------
-- Creating all tables
-- --------------------------------------------------

-- Creating table 'F_nn_Category_Parameters'
CREATE TABLE [dbo].[F_nn_Category_Parameters] (
    [ID_CAT_PARAMETER] int IDENTITY(1,1) NOT NULL,
    [ID_CATEGORY] int  NOT NULL,
    [ID_PARAMETER] int  NOT NULL
);
GO

-- Creating table 'F_nn_Elements_FlatModules'
CREATE TABLE [dbo].[F_nn_Elements_FlatModules] (
    [ID_ELEMENT_IN_FM] int IDENTITY(1,1) NOT NULL,
    [ID_FLAT_MODULE] int  NOT NULL,
    [ID_ELEMENT] int  NOT NULL,
    [LOCATION_POINT] nvarchar(100)  NOT NULL,
    [DIRECTION] nvarchar(100)  NULL
);
GO

-- Creating table 'F_nn_FlatModules'
CREATE TABLE [dbo].[F_nn_FlatModules] (
    [ID_FLAT_MODULE] int IDENTITY(1,1) NOT NULL,
    [ID_FLAT] int  NOT NULL,
    [ID_MODULE] int  NOT NULL,
    [REVISION] int  NOT NULL,
    [LOCATION] nvarchar(100)  NULL,
    [DIRECTION] nvarchar(100)  NULL
);
GO

-- Creating table 'F_R_Flats'
CREATE TABLE [dbo].[F_R_Flats] (
    [ID_FLAT] int IDENTITY(1,1) NOT NULL,
    [COMMERCIAL_NAME] nvarchar(50)  NOT NULL,
    [WORKNAME] nvarchar(150)  NOT NULL
);
GO

-- Creating table 'F_R_Modules'
CREATE TABLE [dbo].[F_R_Modules] (
    [ID_MODULE] int IDENTITY(1,1) NOT NULL,
    [NAME_MODULE] nvarchar(50)  NOT NULL
);
GO

-- Creating table 'F_S_Categories'
CREATE TABLE [dbo].[F_S_Categories] (
    [ID_CATEGORY] int IDENTITY(1,1) NOT NULL,
    [NAME_RUS_CATEGORY] nvarchar(150)  NULL,
    [NAME_ENG_CATEGORY] nvarchar(150)  NULL
);
GO

-- Creating table 'F_S_Elements'
CREATE TABLE [dbo].[F_S_Elements] (
    [ID_ELEMENT] int IDENTITY(1,1) NOT NULL,
    [ID_FAMILY_INFO] int  NOT NULL,
    [ID_CATEGORY] int  NOT NULL
);
GO

-- Creating table 'F_S_FamilyInfos'
CREATE TABLE [dbo].[F_S_FamilyInfos] (
    [ID_FAMILY_INFO] int IDENTITY(1,1) NOT NULL,
    [FAMILY_NAME] nvarchar(100)  NOT NULL,
    [FAMILY_SYMBOL] nvarchar(100)  NOT NULL
);
GO

-- Creating table 'F_S_Parameters'
CREATE TABLE [dbo].[F_S_Parameters] (
    [ID_PARAMETER] int IDENTITY(1,1) NOT NULL,
    [NAME_PARAMETER] nvarchar(150)  NOT NULL,
    [TYPE_PARAMETER] nvarchar(50)  NULL
);
GO

-- Creating table 'F_nn_ElementParam_Value'
CREATE TABLE [dbo].[F_nn_ElementParam_Value] (
    [ID_ELEMENT_IN_FM] int  NOT NULL,
    [ID_CAT_PARAMETER] int  NOT NULL,
    [PARAMETER_VALUE] nvarchar(250)  NOT NULL,
    [ID_ELEMENT_VALUE] int IDENTITY(1,1) NOT NULL
);
GO

-- --------------------------------------------------
-- Creating all PRIMARY KEY constraints
-- --------------------------------------------------

-- Creating primary key on [ID_CAT_PARAMETER] in table 'F_nn_Category_Parameters'
ALTER TABLE [dbo].[F_nn_Category_Parameters]
ADD CONSTRAINT [PK_F_nn_Category_Parameters]
    PRIMARY KEY CLUSTERED ([ID_CAT_PARAMETER] ASC);
GO

-- Creating primary key on [ID_ELEMENT_IN_FM] in table 'F_nn_Elements_FlatModules'
ALTER TABLE [dbo].[F_nn_Elements_FlatModules]
ADD CONSTRAINT [PK_F_nn_Elements_FlatModules]
    PRIMARY KEY CLUSTERED ([ID_ELEMENT_IN_FM] ASC);
GO

-- Creating primary key on [ID_FLAT_MODULE] in table 'F_nn_FlatModules'
ALTER TABLE [dbo].[F_nn_FlatModules]
ADD CONSTRAINT [PK_F_nn_FlatModules]
    PRIMARY KEY CLUSTERED ([ID_FLAT_MODULE] ASC);
GO

-- Creating primary key on [ID_FLAT] in table 'F_R_Flats'
ALTER TABLE [dbo].[F_R_Flats]
ADD CONSTRAINT [PK_F_R_Flats]
    PRIMARY KEY CLUSTERED ([ID_FLAT] ASC);
GO

-- Creating primary key on [ID_MODULE] in table 'F_R_Modules'
ALTER TABLE [dbo].[F_R_Modules]
ADD CONSTRAINT [PK_F_R_Modules]
    PRIMARY KEY CLUSTERED ([ID_MODULE] ASC);
GO

-- Creating primary key on [ID_CATEGORY] in table 'F_S_Categories'
ALTER TABLE [dbo].[F_S_Categories]
ADD CONSTRAINT [PK_F_S_Categories]
    PRIMARY KEY CLUSTERED ([ID_CATEGORY] ASC);
GO

-- Creating primary key on [ID_ELEMENT] in table 'F_S_Elements'
ALTER TABLE [dbo].[F_S_Elements]
ADD CONSTRAINT [PK_F_S_Elements]
    PRIMARY KEY CLUSTERED ([ID_ELEMENT] ASC);
GO

-- Creating primary key on [ID_FAMILY_INFO] in table 'F_S_FamilyInfos'
ALTER TABLE [dbo].[F_S_FamilyInfos]
ADD CONSTRAINT [PK_F_S_FamilyInfos]
    PRIMARY KEY CLUSTERED ([ID_FAMILY_INFO] ASC);
GO

-- Creating primary key on [ID_PARAMETER] in table 'F_S_Parameters'
ALTER TABLE [dbo].[F_S_Parameters]
ADD CONSTRAINT [PK_F_S_Parameters]
    PRIMARY KEY CLUSTERED ([ID_PARAMETER] ASC);
GO

-- Creating primary key on [ID_ELEMENT_VALUE] in table 'F_nn_ElementParam_Value'
ALTER TABLE [dbo].[F_nn_ElementParam_Value]
ADD CONSTRAINT [PK_F_nn_ElementParam_Value]
    PRIMARY KEY CLUSTERED ([ID_ELEMENT_VALUE] ASC);
GO

-- --------------------------------------------------
-- Creating all FOREIGN KEY constraints
-- --------------------------------------------------

-- Creating foreign key on [ID_CATEGORY] in table 'F_nn_Category_Parameters'
ALTER TABLE [dbo].[F_nn_Category_Parameters]
ADD CONSTRAINT [FK_F_nn_Category_Parameters_F_S_Categories]
    FOREIGN KEY ([ID_CATEGORY])
    REFERENCES [dbo].[F_S_Categories]
        ([ID_CATEGORY])
    ON DELETE CASCADE ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_F_nn_Category_Parameters_F_S_Categories'
CREATE INDEX [IX_FK_F_nn_Category_Parameters_F_S_Categories]
ON [dbo].[F_nn_Category_Parameters]
    ([ID_CATEGORY]);
GO

-- Creating foreign key on [ID_PARAMETER] in table 'F_nn_Category_Parameters'
ALTER TABLE [dbo].[F_nn_Category_Parameters]
ADD CONSTRAINT [FK_F_nn_Category_Parameters_F_S_Parameters]
    FOREIGN KEY ([ID_PARAMETER])
    REFERENCES [dbo].[F_S_Parameters]
        ([ID_PARAMETER])
    ON DELETE CASCADE ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_F_nn_Category_Parameters_F_S_Parameters'
CREATE INDEX [IX_FK_F_nn_Category_Parameters_F_S_Parameters]
ON [dbo].[F_nn_Category_Parameters]
    ([ID_PARAMETER]);
GO

-- Creating foreign key on [ID_CAT_PARAMETER] in table 'F_nn_ElementParam_Value'
ALTER TABLE [dbo].[F_nn_ElementParam_Value]
ADD CONSTRAINT [FK_F_nn_ElementParam_Value_F_nn_Category_Parameters]
    FOREIGN KEY ([ID_CAT_PARAMETER])
    REFERENCES [dbo].[F_nn_Category_Parameters]
        ([ID_CAT_PARAMETER])
    ON DELETE CASCADE ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_F_nn_ElementParam_Value_F_nn_Category_Parameters'
CREATE INDEX [IX_FK_F_nn_ElementParam_Value_F_nn_Category_Parameters]
ON [dbo].[F_nn_ElementParam_Value]
    ([ID_CAT_PARAMETER]);
GO

-- Creating foreign key on [ID_ELEMENT_IN_FM] in table 'F_nn_ElementParam_Value'
ALTER TABLE [dbo].[F_nn_ElementParam_Value]
ADD CONSTRAINT [FK_F_nn_ElementParam_Value_F_nn_Elements_FlatModules]
    FOREIGN KEY ([ID_ELEMENT_IN_FM])
    REFERENCES [dbo].[F_nn_Elements_FlatModules]
        ([ID_ELEMENT_IN_FM])
    ON DELETE CASCADE ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_F_nn_ElementParam_Value_F_nn_Elements_FlatModules'
CREATE INDEX [IX_FK_F_nn_ElementParam_Value_F_nn_Elements_FlatModules]
ON [dbo].[F_nn_ElementParam_Value]
    ([ID_ELEMENT_IN_FM]);
GO

-- Creating foreign key on [ID_FLAT_MODULE] in table 'F_nn_Elements_FlatModules'
ALTER TABLE [dbo].[F_nn_Elements_FlatModules]
ADD CONSTRAINT [FK_F_nn_Elements_FlatModules_F_R_FlatModules]
    FOREIGN KEY ([ID_FLAT_MODULE])
    REFERENCES [dbo].[F_nn_FlatModules]
        ([ID_FLAT_MODULE])
    ON DELETE CASCADE ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_F_nn_Elements_FlatModules_F_R_FlatModules'
CREATE INDEX [IX_FK_F_nn_Elements_FlatModules_F_R_FlatModules]
ON [dbo].[F_nn_Elements_FlatModules]
    ([ID_FLAT_MODULE]);
GO

-- Creating foreign key on [ID_ELEMENT] in table 'F_nn_Elements_FlatModules'
ALTER TABLE [dbo].[F_nn_Elements_FlatModules]
ADD CONSTRAINT [FK_F_nn_Elements_FlatModules_F_S_Elements]
    FOREIGN KEY ([ID_ELEMENT])
    REFERENCES [dbo].[F_S_Elements]
        ([ID_ELEMENT])
    ON DELETE CASCADE ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_F_nn_Elements_FlatModules_F_S_Elements'
CREATE INDEX [IX_FK_F_nn_Elements_FlatModules_F_S_Elements]
ON [dbo].[F_nn_Elements_FlatModules]
    ([ID_ELEMENT]);
GO

-- Creating foreign key on [ID_FLAT] in table 'F_nn_FlatModules'
ALTER TABLE [dbo].[F_nn_FlatModules]
ADD CONSTRAINT [FK_F_R_FlatModules_F_R_Flats1]
    FOREIGN KEY ([ID_FLAT])
    REFERENCES [dbo].[F_R_Flats]
        ([ID_FLAT])
    ON DELETE CASCADE ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_F_R_FlatModules_F_R_Flats1'
CREATE INDEX [IX_FK_F_R_FlatModules_F_R_Flats1]
ON [dbo].[F_nn_FlatModules]
    ([ID_FLAT]);
GO

-- Creating foreign key on [ID_MODULE] in table 'F_nn_FlatModules'
ALTER TABLE [dbo].[F_nn_FlatModules]
ADD CONSTRAINT [FK_F_R_FlatModules_F_R_Modules]
    FOREIGN KEY ([ID_MODULE])
    REFERENCES [dbo].[F_R_Modules]
        ([ID_MODULE])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_F_R_FlatModules_F_R_Modules'
CREATE INDEX [IX_FK_F_R_FlatModules_F_R_Modules]
ON [dbo].[F_nn_FlatModules]
    ([ID_MODULE]);
GO

-- Creating foreign key on [ID_CATEGORY] in table 'F_S_Elements'
ALTER TABLE [dbo].[F_S_Elements]
ADD CONSTRAINT [FK_F_S_Elements_F_S_Categories]
    FOREIGN KEY ([ID_CATEGORY])
    REFERENCES [dbo].[F_S_Categories]
        ([ID_CATEGORY])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_F_S_Elements_F_S_Categories'
CREATE INDEX [IX_FK_F_S_Elements_F_S_Categories]
ON [dbo].[F_S_Elements]
    ([ID_CATEGORY]);
GO

-- Creating foreign key on [ID_FAMILY_INFO] in table 'F_S_Elements'
ALTER TABLE [dbo].[F_S_Elements]
ADD CONSTRAINT [FK_F_S_Elements_F_S_FamilyInfos]
    FOREIGN KEY ([ID_FAMILY_INFO])
    REFERENCES [dbo].[F_S_FamilyInfos]
        ([ID_FAMILY_INFO])
    ON DELETE CASCADE ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_F_S_Elements_F_S_FamilyInfos'
CREATE INDEX [IX_FK_F_S_Elements_F_S_FamilyInfos]
ON [dbo].[F_S_Elements]
    ([ID_FAMILY_INFO]);
GO

-- --------------------------------------------------
-- Script has ended
-- --------------------------------------------------