IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105182633_InitialAcademicSchema'
)
BEGIN
    CREATE TABLE [Courses] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(20) NOT NULL,
        [Title] nvarchar(120) NOT NULL,
        [Department] nvarchar(60) NOT NULL,
        [Credits] int NOT NULL,
        [Capacity] int NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Courses] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Course_Capacity] CHECK ([Capacity] BETWEEN 1 AND 500),
        CONSTRAINT [CK_Course_Credits] CHECK ([Credits] BETWEEN 1 AND 6),
        CONSTRAINT [CK_Course_Department] CHECK ([Department] IN ('Business','Computer Science','Design','Engineering')),
        CONSTRAINT [CK_Course_Status] CHECK ([Status] IN ('Active','Archived')),
        CONSTRAINT [CK_Course_Text] CHECK (LEN(LTRIM(RTRIM([Code]))) > 0 AND LEN(LTRIM(RTRIM([Title]))) > 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105182633_InitialAcademicSchema'
)
BEGIN
    CREATE TABLE [Students] (
        [Id] int NOT NULL IDENTITY,
        [Number] nvarchar(30) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Email] nvarchar(254) NOT NULL,
        [Department] nvarchar(60) NOT NULL,
        [Year] int NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Students] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Student_Department] CHECK ([Department] IN ('Business','Computer Science','Design','Engineering')),
        CONSTRAINT [CK_Student_Status] CHECK ([Status] IN ('Active','On leave','Graduated')),
        CONSTRAINT [CK_Student_Text] CHECK (LEN(LTRIM(RTRIM([Number]))) > 0 AND LEN(LTRIM(RTRIM([Name]))) > 0 AND LEN(LTRIM(RTRIM([Email]))) > 0),
        CONSTRAINT [CK_Student_Year] CHECK ([Year] BETWEEN 1 AND 4)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105182633_InitialAcademicSchema'
)
BEGIN
    CREATE TABLE [Enrollments] (
        [Id] int NOT NULL IDENTITY,
        [StudentId] int NOT NULL,
        [CourseId] int NOT NULL,
        [Term] nvarchar(12) NOT NULL,
        [AcademicYear] int NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [Grade] nvarchar(2) NULL,
        [EnrolledOn] date NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Enrollments] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Enrollment_Grade] CHECK (([Status] = 'Completed' AND [Grade] IS NOT NULL AND [Grade] IN ('A','A-','B+','B','B-','C+','C','C-','D','F')) OR ([Status] <> 'Completed' AND [Grade] IS NULL)),
        CONSTRAINT [CK_Enrollment_Status] CHECK ([Status] IN ('Enrolled','Completed','Withdrawn')),
        CONSTRAINT [CK_Enrollment_Term] CHECK ([Term] IN ('Spring','Summer','Fall','Winter')),
        CONSTRAINT [CK_Enrollment_Year] CHECK ([AcademicYear] BETWEEN 2000 AND 2100 AND YEAR([EnrolledOn]) = [AcademicYear]),
        CONSTRAINT [FK_Enrollments_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Enrollments_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105182633_InitialAcademicSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Courses_Code] ON [Courses] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105182633_InitialAcademicSchema'
)
BEGIN
    CREATE INDEX [IX_Enrollments_CourseId_Term_AcademicYear_Status] ON [Enrollments] ([CourseId], [Term], [AcademicYear], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105182633_InitialAcademicSchema'
)
BEGIN
    CREATE INDEX [IX_Enrollments_EnrolledOn] ON [Enrollments] ([EnrolledOn]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105182633_InitialAcademicSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Enrollments_StudentId_CourseId_Term_AcademicYear] ON [Enrollments] ([StudentId], [CourseId], [Term], [AcademicYear]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105182633_InitialAcademicSchema'
)
BEGIN
    CREATE INDEX [IX_Students_Department] ON [Students] ([Department]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105182633_InitialAcademicSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Students_Email] ON [Students] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105182633_InitialAcademicSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Students_Number] ON [Students] ([Number]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105182633_InitialAcademicSchema'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261105182633_InitialAcademicSchema', N'10.0.14');
END;

COMMIT;
GO
