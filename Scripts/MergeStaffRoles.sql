-- Run once against the application's MySQL/TiDB database.
UPDATE users
SET Role = 'Staff'
WHERE Role IN ('Receptionist', 'Chef', 'StockChecker');
