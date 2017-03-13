CREATE TABLE [dbo].[<Keep Name>Keep](
	[MessageId] [bigint] IDENTITY(1,1) NOT NULL,
	[OriginalStoreTime] [datetimeoffset](7) NOT NULL,
	[LastStoreTime] [datetimeoffset](7) NOT NULL,
	[StoreCount] [smallint] NOT NULL,
	[Payload] [nvarchar](max) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[MessageId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
)