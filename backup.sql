-- MySQL dump 10.13  Distrib 26.7.0, for Win64 (x86_64)
--
-- Host: localhost    Database: isanderdb
-- ------------------------------------------------------
-- Server version	26.7.0

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Current Database: `isanderdb`
--

CREATE DATABASE /*!32312 IF NOT EXISTS*/ `isanderdb` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci */ /*!80016 DEFAULT ENCRYPTION='N' */;

USE `isanderdb`;

--
-- Table structure for table `__efmigrationshistory`
--

DROP TABLE IF EXISTS `__efmigrationshistory`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `__efmigrationshistory` (
  `MigrationId` varchar(150) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ProductVersion` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  PRIMARY KEY (`MigrationId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `__efmigrationshistory`
--

LOCK TABLES `__efmigrationshistory` WRITE;
/*!40000 ALTER TABLE `__efmigrationshistory` DISABLE KEYS */;
/*!40000 ALTER TABLE `__efmigrationshistory` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `buffet_child_promotions`
--

DROP TABLE IF EXISTS `buffet_child_promotions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `buffet_child_promotions` (
  `PromotionID` tinyint unsigned NOT NULL,
  `Code` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `StartDate` date DEFAULT NULL,
  `EndDate` date DEFAULT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT '0',
  PRIMARY KEY (`PromotionID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `buffet_child_promotions`
--

LOCK TABLES `buffet_child_promotions` WRITE;
/*!40000 ALTER TABLE `buffet_child_promotions` DISABLE KEYS */;
INSERT INTO `buffet_child_promotions` VALUES (1,NULL,NULL,NULL,0);
/*!40000 ALTER TABLE `buffet_child_promotions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `dining_tables`
--

DROP TABLE IF EXISTS `dining_tables`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `dining_tables` (
  `TableID` int NOT NULL AUTO_INCREMENT,
  `TableNumber` varchar(10) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Capacity` int NOT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`TableID`),
  UNIQUE KEY `UX_dining_tables_TableNumber` (`TableNumber`)
) ENGINE=InnoDB AUTO_INCREMENT=9 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `dining_tables`
--

LOCK TABLES `dining_tables` WRITE;
/*!40000 ALTER TABLE `dining_tables` DISABLE KEYS */;
INSERT INTO `dining_tables` VALUES (1,'01',4,1),(2,'02',4,1),(3,'03',4,1),(4,'04',4,1),(5,'05',2,1),(6,'06',2,1),(7,'07',2,1),(8,'08',2,1);
/*!40000 ALTER TABLE `dining_tables` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `home_promotions`
--

DROP TABLE IF EXISTS `home_promotions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `home_promotions` (
  `PromotionID` int NOT NULL AUTO_INCREMENT,
  `SlotKey` varchar(40) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Kicker` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Headline` varchar(200) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Description` varchar(500) COLLATE utf8mb4_unicode_ci NOT NULL,
  `LinkText` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  `SortOrder` int NOT NULL DEFAULT '0',
  PRIMARY KEY (`PromotionID`),
  UNIQUE KEY `UX_home_promotions_SlotKey` (`SlotKey`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `home_promotions`
--

LOCK TABLES `home_promotions` WRITE;
/*!40000 ALTER TABLE `home_promotions` DISABLE KEYS */;
INSERT INTO `home_promotions` VALUES (1,'membership','สิทธิพิเศษสำหรับสมาชิก','สมัครสมาชิก\nรับชามะลิฟรี','ร่วมเป็นสมาชิกอีสานเด้อเพื่อรับสิทธิพิเศษ','สมัครสมาชิก',1,1),(2,'buffet','โปรวันพิเศษ','เด็กอายุไม่เกิน 10 ปี\nกินฟรีเมื่อแต่งกายตามธีม','รับโค้ดส่วนลดบุฟเฟต์จากร้าน แล้วกรอกก่อนชำระเงิน','ดูแพ็กเกจบุฟเฟต์',1,2);
/*!40000 ALTER TABLE `home_promotions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `menus`
--

DROP TABLE IF EXISTS `menus`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `menus` (
  `MenuID` int NOT NULL AUTO_INCREMENT,
  `MenuName` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Price` decimal(10,2) NOT NULL,
  `Status` varchar(20) COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'Available',
  `StockQuantity` int DEFAULT NULL,
  PRIMARY KEY (`MenuID`)
) ENGINE=InnoDB AUTO_INCREMENT=52 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `menus`
--

LOCK TABLES `menus` WRITE;
/*!40000 ALTER TABLE `menus` DISABLE KEYS */;
INSERT INTO `menus` VALUES (1,'ส้มตำไทย',60.00,'Available',NULL),(2,'ลาบหมู',90.00,'Available',NULL),(3,'คอหมูย่าง',120.00,'Available',NULL),(4,'ต้มแซ่บกระดูกอ่อน',100.00,'Available',NULL),(5,'ไก่ย่าง',80.00,'Available',NULL),(6,'ข้าวเหนียว',15.00,'Available',NULL);
/*!40000 ALTER TABLE `menus` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `order_items`
--

DROP TABLE IF EXISTS `order_items`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `order_items` (
  `OrderItemID` int NOT NULL AUTO_INCREMENT,
  `OrderID` int NOT NULL,
  `MenuID` int NOT NULL,
  `Quantity` int NOT NULL,
  `Price` decimal(10,2) NOT NULL,
  `StockDeducted` tinyint(1) NOT NULL DEFAULT '0',
  PRIMARY KEY (`OrderItemID`)
) ENGINE=InnoDB AUTO_INCREMENT=17 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `order_items`
--

LOCK TABLES `order_items` WRITE;
/*!40000 ALTER TABLE `order_items` DISABLE KEYS */;
INSERT INTO `order_items` VALUES (1,1,1,1,60.00,0),(2,2,1,1,60.00,0),(3,3,1,1,60.00,0),(4,4,1,1,60.00,0),(5,5,1,1,60.00,0),(6,6,1,1,60.00,0),(7,6,2,1,90.00,0),(8,7,1,2,60.00,0),(9,8,1,1,60.00,0),(10,9,1,1,60.00,0),(11,10,1,1,60.00,0),(12,10,2,1,90.00,0),(13,11,1,1,60.00,0),(14,11,2,1,90.00,0),(15,13,1,1,60.00,0),(16,13,2,1,90.00,0);
/*!40000 ALTER TABLE `order_items` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `orders`
--

DROP TABLE IF EXISTS `orders`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `orders` (
  `OrderID` int NOT NULL AUTO_INCREMENT,
  `UserID` int NOT NULL,
  `OrderDate` datetime DEFAULT CURRENT_TIMESTAMP,
  `OrderType` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `ArriveTime` varchar(20) COLLATE utf8mb4_unicode_ci NOT NULL,
  `PaymentMethod` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `TotalAmount` decimal(10,2) NOT NULL,
  `OrderStatus` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT 'รอดำเนินการ',
  `TableID` int DEFAULT NULL,
  `PaymentStatus` varchar(30) COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'รอชำระเงิน',
  `BuffetGuestCount` int DEFAULT NULL,
  `BuffetFreeChildCount` int NOT NULL DEFAULT '0',
  `PromoCode` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `DiscountAmount` decimal(10,2) NOT NULL DEFAULT '0.00',
  PRIMARY KEY (`OrderID`),
  KEY `IX_orders_TableID` (`TableID`),
  CONSTRAINT `FK_orders_dining_tables_TableID` FOREIGN KEY (`TableID`) REFERENCES `dining_tables` (`TableID`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=14 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `orders`
--

LOCK TABLES `orders` WRITE;
/*!40000 ALTER TABLE `orders` DISABLE KEYS */;
INSERT INTO `orders` VALUES (1,2,'2026-11-17 10:35:41','ทานที่ร้าน','10:35','เงินสด (ชำระที่หน้าร้าน)',60.00,'รอดำเนินการ',NULL,'รอชำระเงิน',NULL,0,NULL,0.00),(2,2,'2026-11-17 10:51:24','ทานที่ร้าน','10:51','เงินสด (ชำระที่หน้าร้าน)',60.00,'รอดำเนินการ',NULL,'รอชำระเงิน',NULL,0,NULL,0.00),(3,2,'2026-11-17 11:06:38','ทานที่ร้าน','11:06','เงินสด (ชำระที่หน้าร้าน)',60.00,'รอดำเนินการ',NULL,'รอชำระเงิน',NULL,0,NULL,0.00),(4,2,'2026-11-17 11:07:14','ทานที่ร้าน','11:07','เงินสด (ชำระที่หน้าร้าน)',60.00,'รอดำเนินการ',NULL,'รอชำระเงิน',NULL,0,NULL,0.00),(5,2,'2026-11-17 11:16:40','ทานที่ร้าน','11:16','เงินสด (ชำระที่หน้าร้าน)',60.00,'รอดำเนินการ',NULL,'รอชำระเงิน',NULL,0,NULL,0.00),(6,3,'2026-11-17 11:50:40','ทานที่ร้าน','11:50','เงินสด (ชำระที่หน้าร้าน)',150.00,'รอดำเนินการ',NULL,'รอชำระเงิน',NULL,0,NULL,0.00),(7,3,'2026-11-17 12:40:18','สั่งล่วงหน้า รับที่ร้าน','12:41','PromptPay (โอนเงิน)',120.00,'รอดำเนินการ',NULL,'รอชำระเงิน',NULL,0,NULL,0.00),(8,3,'2026-11-17 12:40:40','สั่งล่วงหน้า รับที่ร้าน','12:41','เงินสด (ชำระที่หน้าร้าน)',60.00,'รอดำเนินการ',NULL,'รอชำระเงิน',NULL,0,NULL,0.00),(9,5,'2026-11-19 07:45:38','ทานที่ร้าน','-','เงินสด (ชำระที่หน้าร้าน)',60.00,'เสร็จแล้ว',6,'รอชำระเงิน',NULL,0,NULL,0.00),(10,5,'2026-11-19 08:04:21','ทานที่ร้าน','-','PromptPay (จำลอง)',150.00,'รอดำเนินการ',2,'รอชำระเงิน',NULL,0,NULL,0.00),(11,5,'2026-11-19 09:03:10','ทานที่ร้าน','-','PromptPay',150.00,'เสร็จแล้ว',2,'ชำระแล้ว',NULL,0,NULL,0.00),(12,5,'2026-11-20 02:28:39','บุฟเฟต์','-','เงินสด (ชำระที่หน้าร้าน)',299.00,'รอดำเนินการ',2,'รอชำระเงิน',1,0,NULL,0.00),(13,5,'2026-11-20 02:29:06','ทานที่ร้าน','-','เงินสด (ชำระที่หน้าร้าน)',150.00,'รอดำเนินการ',2,'รอชำระเงิน',NULL,0,NULL,0.00);
/*!40000 ALTER TABLE `orders` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `users`
--

DROP TABLE IF EXISTS `users`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `users` (
  `UserID` int NOT NULL AUTO_INCREMENT,
  `Username` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `PasswordHash` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Role` varchar(20) COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'Customer',
  `Email` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `PhoneNumber` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `DateOfBirth` date DEFAULT NULL,
  `MarketingConsent` tinyint(1) NOT NULL DEFAULT '0',
  `TermsAcceptedAt` datetime DEFAULT NULL,
  PRIMARY KEY (`UserID`),
  UNIQUE KEY `Email` (`Email`)
) ENGINE=InnoDB AUTO_INCREMENT=9 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `users`
--

LOCK TABLES `users` WRITE;
/*!40000 ALTER TABLE `users` DISABLE KEYS */;
INSERT INTO `users` VALUES (1,'line man','1234','Customer','lineman@gmail.com',NULL,NULL,0,NULL),(2,'line man1','AQAAAAIAAYagAAAAEGY5dmKDfVZqoNh0X9iHwJAtz/TjVtSJ6aSkn4I+Jsm/huxZuuWbA+TZQfcaUt1iQw==','Manager','lineman1@gmail.com',NULL,NULL,0,NULL),(3,'line man2','AQAAAAIAAYagAAAAEHHgYDtcO0vT7Tg0fL8Ofz8N0gbrWsEKCXKhp7v/FAdNuXXyPJMyhPfAsauRvbCtPg==','Customer','lineman2@gmail.com','1111111111','2005-08-17',1,'2026-11-17 04:32:33'),(4,'admin','AQAAAAIAAYagAAAAEELeCBKL+oJqgVCBALG9jmXK+6TUzG3ECUPZjAHhjACIfwmrSi54F37vEYsT/6C0HA==','Admin','admin1@admin.com',NULL,NULL,1,'2026-11-17 06:45:29'),(5,'test','AQAAAAIAAYagAAAAEBBjUZ1+VeIc9F9sagisyzTJEHPJcRexMsnsVHo9oN6VH+d0vpcTK0WbwaZt133xbA==','Customer','test@admin.com',NULL,NULL,1,'2026-11-17 06:47:51'),(6,'manager1','AQAAAAIAAYagAAAAENgU9dQH9PSPdw7JxDs/wsWmbQ8oK98OLJJ2a5gMyKgbI4C4414rOQd2GpgS4aNgaw==','Manager','manager1@gmail.com',NULL,NULL,0,NULL),(7,'Staff1','AQAAAAIAAYagAAAAEGkSn7zH4GYeb+cFRZzWvk6AmfwF4374GlyLe1l1k1QUgoIibRyq/XTyfJIkorRTgw==','StockChecker','staff1@gmail.com',NULL,NULL,0,NULL),(8,'Staff2','AQAAAAIAAYagAAAAEBZT9/oUCLWkrB/uXBBAvD4Oek8u5AQ+e6Mf5sfQENRrrOSMK4aHUG9C7T8gDtWzbQ==','Chef','staff2@gmail.com',NULL,NULL,0,NULL);
/*!40000 ALTER TABLE `users` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-11-20  3:38:55
