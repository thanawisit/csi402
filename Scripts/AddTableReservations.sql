-- Run once against the application's MySQL database.
CREATE TABLE IF NOT EXISTS table_reservations (
    ReservationID INT NOT NULL AUTO_INCREMENT,
    UserID INT NOT NULL,
    TableID INT NOT NULL,
    Package VARCHAR(20) NOT NULL,
    ReservationDateTime DATETIME NOT NULL,
    ReservationEndTime DATETIME NOT NULL,
    GuestCount INT NOT NULL,
    CustomerName VARCHAR(50) NOT NULL,
    CustomerEmail VARCHAR(100) NOT NULL,
    CustomerPhone VARCHAR(20) NOT NULL,
    Note VARCHAR(500) NULL,
    Status VARCHAR(30) NOT NULL DEFAULT 'รอยืนยัน',
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (ReservationID),
    KEY IX_table_reservations_TableID_ReservationDateTime (TableID, ReservationDateTime),
    KEY IX_table_reservations_UserID (UserID),
    CONSTRAINT FK_table_reservations_users
        FOREIGN KEY (UserID) REFERENCES users (UserID),
    CONSTRAINT FK_table_reservations_dining_tables
        FOREIGN KEY (TableID) REFERENCES dining_tables (TableID)
);
