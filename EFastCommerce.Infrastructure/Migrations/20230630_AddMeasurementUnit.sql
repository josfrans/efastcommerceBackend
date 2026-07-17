BEGIN TRANSACTION;
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Products' AND COLUMN_NAME = 'MeasurementUnit')
BEGIN
    ALTER TABLE Products ADD MeasurementUnit NVARCHAR(20) NOT NULL DEFAULT 'Piece';
    -- Update existing rows if needed (already defaulted)
END
COMMIT;
