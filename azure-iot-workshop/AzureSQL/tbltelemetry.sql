USE [telemetry]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

GO
CREATE TABLE [dbo].[tbltelemetry] (
    [DeviceId]    NVARCHAR (50) NULL,
    [TimeStamp]   DATETIME2 (7) NULL,
    [Humidity]    FLOAT (53)    NULL,
    [Temperature] FLOAT (53)    NULL,
    [WindSpeed]   FLOAT (53)    NULL,
    [Raining]     SMALLINT      NULL
);


