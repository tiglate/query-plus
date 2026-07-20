-- QueryPlus demo objects for integration testing
-- Idempotent: safe to re-run


IF OBJECT_ID('dbo.tb_usa_president', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tb_usa_president (
        id_president INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
        full_name VARCHAR(120) NOT NULL,
        party VARCHAR(50) NOT NULL,
        birth_state VARCHAR(50) NOT NULL,
        birth_date DATE NULL,
        term_start DATE NOT NULL,
        term_end DATE NULL,
        term_number INT NOT NULL
    );
END

IF NOT EXISTS (SELECT 1 FROM dbo.tb_usa_president)
BEGIN
    SET IDENTITY_INSERT dbo.tb_usa_president OFF;
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'George Washington', N'Independent', N'Virginia', '1732-02-22', '1789-04-30', '1797-03-04', 1);
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'John Adams', N'Federalist', N'Massachusetts', '1735-10-30', '1797-03-04', '1801-03-04', 2);
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'Thomas Jefferson', N'Democratic-Republican', N'Virginia', '1743-04-13', '1801-03-04', '1809-03-04', 3);
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'James Madison', N'Democratic-Republican', N'Virginia', '1751-03-16', '1809-03-04', '1817-03-04', 4);
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'James Monroe', N'Democratic-Republican', N'Virginia', '1758-04-28', '1817-03-04', '1825-03-04', 5);
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'John Quincy Adams', N'Democratic-Republican', N'Massachusetts', '1767-07-11', '1825-03-04', '1829-03-04', 6);
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'Andrew Jackson', N'Democratic', N'South Carolina', '1767-03-15', '1829-03-04', '1837-03-04', 7);
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'Martin Van Buren', N'Democratic', N'New York', '1782-12-05', '1837-03-04', '1841-03-04', 8);
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'William Henry Harrison', N'Whig', N'Virginia', '1773-02-09', '1841-03-04', '1841-04-04', 9);
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'John Tyler', N'Whig', N'Virginia', '1790-03-29', '1841-04-04', '1845-03-04', 10);
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'James K. Polk', N'Democratic', N'North Carolina', '1795-11-02', '1845-03-04', '1849-03-04', 11);
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'Zachary Taylor', N'Whig', N'Virginia', '1784-11-24', '1849-03-04', '1850-07-09', 12);
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'Millard Fillmore', N'Whig', N'New York', '1800-01-07', '1850-07-09', '1853-03-04', 13);
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'Franklin Pierce', N'Democratic', N'New Hampshire', '1804-11-23', '1853-03-04', '1857-03-04', 14);
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'James Buchanan', N'Democratic', N'Pennsylvania', '1791-04-23', '1857-03-04', '1861-03-04', 15);
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'Abraham Lincoln', N'Republican', N'Kentucky', '1809-02-12', '1861-03-04', '1865-04-15', 16);
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'Andrew Johnson', N'Democratic', N'North Carolina', '1808-12-29', '1865-04-15', '1869-03-04', 17);
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'Ulysses S. Grant', N'Republican', N'Ohio', '1822-04-27', '1869-03-04', '1877-03-04', 18);
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'Rutherford B. Hayes', N'Republican', N'Ohio', '1822-10-04', '1877-03-04', '1881-03-04', 19);
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'James A. Garfield', N'Republican', N'Ohio', '1831-11-19', '1881-03-04', '1881-09-19', 20);
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'Chester A. Arthur', N'Republican', N'Vermont', '1829-10-05', '1881-09-19', '1885-03-04', 21);
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'Grover Cleveland', N'Democratic', N'New Jersey', '1837-03-18', '1885-03-04', '1889-03-04', 22);
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'Benjamin Harrison', N'Republican', N'Ohio', '1833-08-20', '1889-03-04', '1893-03-04', 23);
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'Grover Cleveland', N'Democratic', N'New Jersey', '1837-03-18', '1893-03-04', '1897-03-04', 24);
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'William McKinley', N'Republican', N'Ohio', '1843-01-29', '1897-03-04', '1901-09-14', 25);
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'Theodore Roosevelt', N'Republican', N'New York', '1858-10-27', '1901-09-14', '1909-03-04', 26);
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'William Howard Taft', N'Republican', N'Ohio', '1857-09-15', '1909-03-04', '1913-03-04', 27);
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'Woodrow Wilson', N'Democratic', N'Virginia', '1856-12-28', '1913-03-04', '1921-03-04', 28);
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'Warren G. Harding', N'Republican', N'Ohio', '1865-11-02', '1921-03-04', '1923-08-02', 29);
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'Calvin Coolidge', N'Republican', N'Vermont', '1872-07-04', '1923-08-02', '1929-03-04', 30);
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'Herbert Hoover', N'Republican', N'Iowa', '1874-08-10', '1929-03-04', '1933-03-04', 31);
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'Franklin D. Roosevelt', N'Democratic', N'New York', '1882-01-30', '1933-03-04', '1945-04-12', 32);
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'Harry S. Truman', N'Democratic', N'Missouri', '1884-05-08', '1945-04-12', '1953-01-20', 33);
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'Dwight D. Eisenhower', N'Republican', N'Texas', '1890-10-14', '1953-01-20', '1961-01-20', 34);
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'John F. Kennedy', N'Democratic', N'Massachusetts', '1917-05-29', '1961-01-20', '1963-11-22', 35);
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'Lyndon B. Johnson', N'Democratic', N'Texas', '1908-08-27', '1963-11-22', '1969-01-20', 36);
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'Richard Nixon', N'Republican', N'California', '1913-01-09', '1969-01-20', '1974-08-09', 37);
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'Gerald Ford', N'Republican', N'Nebraska', '1913-07-14', '1974-08-09', '1977-01-20', 38);
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'Jimmy Carter', N'Democratic', N'Georgia', '1924-10-01', '1977-01-20', '1981-01-20', 39);
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'Ronald Reagan', N'Republican', N'Illinois', '1911-02-06', '1981-01-20', '1989-01-20', 40);
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'George H. W. Bush', N'Republican', N'Massachusetts', '1924-06-12', '1989-01-20', '1993-01-20', 41);
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'Bill Clinton', N'Democratic', N'Arkansas', '1946-08-19', '1993-01-20', '2001-01-20', 42);
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'George W. Bush', N'Republican', N'Connecticut', '1946-07-06', '2001-01-20', '2009-01-20', 43);
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'Barack Obama', N'Democratic', N'Hawaii', '1961-08-04', '2009-01-20', '2017-01-20', 44);
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'Donald Trump', N'Republican', N'New York', '1946-06-14', '2017-01-20', '2021-01-20', 45);
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'Joe Biden', N'Democratic', N'Pennsylvania', '1942-11-20', '2021-01-20', '2025-01-20', 46);
    INSERT INTO dbo.tb_usa_president (full_name, party, birth_state, birth_date, term_start, term_end, term_number)
    VALUES (N'Donald Trump', N'Republican', N'New York', '1946-06-14', '2025-01-20', NULL, 47);
END


IF OBJECT_ID('dbo.tb_demo_customer', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tb_demo_customer (
        id_customer INT IDENTITY(1,1) PRIMARY KEY,
        code VARCHAR(20) NOT NULL,
        full_name VARCHAR(120) NOT NULL,
        city VARCHAR(80) NOT NULL,
        country VARCHAR(60) NOT NULL,
        is_active BIT NOT NULL DEFAULT(1),
        credit_limit DECIMAL(18,2) NOT NULL,
        created_at DATETIME2 NOT NULL DEFAULT(SYSDATETIME())
    );
    INSERT INTO dbo.tb_demo_customer (code, full_name, city, country, is_active, credit_limit, created_at) VALUES
    ('C001','Acme Corp','New York','USA',1,250000.00,'2020-01-15'),
    ('C002','Globex Ltd','London','UK',1,180000.50,'2020-03-22'),
    ('C003','Initech','Austin','USA',0,50000.00,'2019-07-01'),
    ('C004','Umbrella SA','São Paulo','Brazil',1,320000.75,'2021-11-09'),
    ('C005','Stark Industries','Los Angeles','USA',1,999999.99,'2018-05-04'),
    ('C006','Wayne Enterprises','Gotham','USA',1,750000.00,'2018-08-12'),
    ('C007','Oscorp','New York','USA',0,120000.00,'2022-02-28'),
    ('C008','Hooli','Palo Alto','USA',1,410000.25,'2021-06-18'),
    ('C009','Pied Piper','Silicon Valley','USA',1,90000.00,'2023-01-10'),
    ('C010','Massive Dynamic','Cambridge','USA',1,560000.00,'2017-09-30');
END

IF OBJECT_ID('dbo.tb_demo_product', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tb_demo_product (
        id_product INT IDENTITY(1,1) PRIMARY KEY,
        sku VARCHAR(30) NOT NULL,
        product_name VARCHAR(120) NOT NULL,
        category VARCHAR(50) NOT NULL,
        unit_price DECIMAL(18,2) NOT NULL,
        stock_qty INT NOT NULL,
        discount_pct DECIMAL(5,2) NOT NULL,
        is_discontinued BIT NOT NULL DEFAULT(0)
    );
    INSERT INTO dbo.tb_demo_product (sku, product_name, category, unit_price, stock_qty, discount_pct, is_discontinued) VALUES
    ('SKU-100','Wireless Mouse','Accessories',29.90,150,0.00,0),
    ('SKU-101','Mechanical Keyboard','Accessories',119.00,80,5.00,0),
    ('SKU-200','27in Monitor','Displays',349.99,40,10.00,0),
    ('SKU-201','Ultrawide Monitor','Displays',699.00,15,12.50,0),
    ('SKU-300','Laptop Pro 14','Computers',1899.00,25,0.00,0),
    ('SKU-301','Laptop Air 13','Computers',1299.00,35,7.50,0),
    ('SKU-400','USB-C Hub','Accessories',49.90,200,0.00,0),
    ('SKU-500','Legacy Dock','Accessories',89.00,5,25.00,1),
    ('SKU-600','Noise Cancelling Headphones','Audio',249.00,60,15.00,0),
    ('SKU-601','Desktop Speakers','Audio',79.50,90,0.00,0);
END

IF OBJECT_ID('dbo.tb_demo_order', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tb_demo_order (
        id_order INT IDENTITY(1,1) PRIMARY KEY,
        id_customer INT NOT NULL,
        order_date DATE NOT NULL,
        ship_date DATE NULL,
        status VARCHAR(20) NOT NULL,
        total_amount DECIMAL(18,2) NOT NULL,
        tax_amount DECIMAL(18,2) NOT NULL
    );
    INSERT INTO dbo.tb_demo_order (id_customer, order_date, ship_date, status, total_amount, tax_amount) VALUES
    (1,'2024-01-10','2024-01-12','Shipped',1500.00,120.00),
    (1,'2024-03-05','2024-03-07','Shipped',420.50,33.64),
    (2,'2024-02-14','2024-02-16','Shipped',890.00,71.20),
    (3,'2024-04-01',NULL,'Pending',210.00,16.80),
    (4,'2024-05-20','2024-05-22','Shipped',5600.00,448.00),
    (5,'2024-06-11','2024-06-13','Shipped',9999.99,800.00),
    (6,'2024-07-02',NULL,'Cancelled',0.00,0.00),
    (7,'2024-08-15','2024-08-18','Shipped',330.25,26.42),
    (8,'2025-01-09','2025-01-11','Shipped',1780.00,142.40),
    (9,'2025-02-21',NULL,'Pending',95.00,7.60),
    (10,'2025-03-03','2025-03-05','Shipped',2450.75,196.06),
    (2,'2025-04-12','2025-04-14','Shipped',640.00,51.20);
END

IF OBJECT_ID('dbo.tb_demo_employee', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tb_demo_employee (
        id_employee INT IDENTITY(1,1) PRIMARY KEY,
        employee_code VARCHAR(20) NOT NULL,
        full_name VARCHAR(120) NOT NULL,
        department VARCHAR(50) NOT NULL,
        job_title VARCHAR(80) NOT NULL,
        hire_date DATE NOT NULL,
        salary DECIMAL(18,2) NOT NULL,
        is_manager BIT NOT NULL DEFAULT(0),
        work_start_time TIME NOT NULL,
        work_end_time TIME NOT NULL
    );
    INSERT INTO dbo.tb_demo_employee (employee_code, full_name, department, job_title, hire_date, salary, is_manager, work_start_time, work_end_time) VALUES
    ('E001','Alice Johnson','Engineering','Staff Engineer','2018-03-01',145000.00,0,'09:00','18:00'),
    ('E002','Bob Smith','Engineering','Engineering Manager','2016-07-15',175000.00,1,'08:30','17:30'),
    ('E003','Carol Lee','Sales','Account Executive','2019-11-20',92000.00,0,'09:00','18:00'),
    ('E004','Diego Alvarez','Sales','Sales Director','2015-01-10',160000.00,1,'08:00','17:00'),
    ('E005','Emma Brown','Finance','Financial Analyst','2020-05-05',88000.00,0,'09:00','18:00'),
    ('E006','Farah Khan','Finance','CFO','2014-09-01',240000.00,1,'08:00','17:00'),
    ('E007','Gabe Martins','HR','HR Specialist','2021-02-14',72000.00,0,'09:00','18:00'),
    ('E008','Hannah Ng','HR','HR Manager','2017-06-30',110000.00,1,'08:30','17:30'),
    ('E009','Ivan Petrov','Operations','Ops Coordinator','2022-08-08',68000.00,0,'07:00','16:00'),
    ('E010','Julia Costa','Operations','Ops Manager','2018-12-01',125000.00,1,'07:30','16:30');
END

IF OBJECT_ID('dbo.tb_demo_invoice', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tb_demo_invoice (
        id_invoice INT IDENTITY(1,1) PRIMARY KEY,
        invoice_number VARCHAR(30) NOT NULL,
        id_customer INT NOT NULL,
        issue_date DATE NOT NULL,
        due_date DATE NOT NULL,
        paid_at DATETIME2 NULL,
        amount DECIMAL(18,2) NOT NULL,
        currency VARCHAR(3) NOT NULL,
        is_paid BIT NOT NULL
    );
    INSERT INTO dbo.tb_demo_invoice (invoice_number, id_customer, issue_date, due_date, paid_at, amount, currency, is_paid) VALUES
    ('INV-2024-001',1,'2024-01-15','2024-02-15','2024-02-10T14:30:00',1500.00,'USD',1),
    ('INV-2024-002',2,'2024-02-20','2024-03-20','2024-03-18T09:15:00',890.00,'GBP',1),
    ('INV-2024-003',4,'2024-05-25','2024-06-25',NULL,5600.00,'BRL',0),
    ('INV-2024-004',5,'2024-06-15','2024-07-15','2024-07-01T16:45:00',9999.99,'USD',1),
    ('INV-2025-001',8,'2025-01-12','2025-02-12',NULL,1780.00,'USD',0),
    ('INV-2025-002',10,'2025-03-08','2025-04-08','2025-03-30T11:00:00',2450.75,'USD',1);
END

IF OBJECT_ID('dbo.tb_demo_event', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tb_demo_event (
        id_event INT IDENTITY(1,1) PRIMARY KEY,
        event_name VARCHAR(120) NOT NULL,
        event_type VARCHAR(40) NOT NULL,
        event_date DATE NOT NULL,
        start_time TIME NOT NULL,
        end_time TIME NOT NULL,
        location VARCHAR(100) NOT NULL,
        capacity INT NOT NULL,
        is_public BIT NOT NULL
    );
    INSERT INTO dbo.tb_demo_event (event_name, event_type, event_date, start_time, end_time, location, capacity, is_public) VALUES
    ('Tech Summit 2025','Conference','2025-09-10','09:00','18:00','Convention Center',1200,1),
    ('Sales Kickoff','Internal','2025-01-20','08:30','12:30','HQ Auditorium',300,0),
    ('Product Launch','Marketing','2025-06-05','14:00','16:00','Online',5000,1),
    ('Security Workshop','Training','2025-04-15','10:00','13:00','Training Room B',40,0),
    ('Customer Meetup','Community','2025-11-02','18:00','21:00','City Hall',200,1);
END

IF OBJECT_ID('dbo.tb_demo_sensor', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tb_demo_sensor (
        id_reading INT IDENTITY(1,1) PRIMARY KEY,
        sensor_code VARCHAR(30) NOT NULL,
        location VARCHAR(80) NOT NULL,
        reading_at DATETIME2 NOT NULL,
        temperature_c DECIMAL(6,2) NOT NULL,
        humidity_pct DECIMAL(5,2) NOT NULL,
        is_alert BIT NOT NULL
    );
    INSERT INTO dbo.tb_demo_sensor (sensor_code, location, reading_at, temperature_c, humidity_pct, is_alert) VALUES
    ('S-A1','Server Room','2025-03-01T08:00:00',22.50,45.00,0),
    ('S-A1','Server Room','2025-03-01T12:00:00',24.10,47.20,0),
    ('S-A1','Server Room','2025-03-01T16:00:00',29.80,55.00,1),
    ('S-B2','Warehouse','2025-03-01T08:00:00',18.20,60.00,0),
    ('S-B2','Warehouse','2025-03-01T12:00:00',21.00,58.50,0),
    ('S-C3','Office','2025-03-01T09:00:00',23.00,40.00,0),
    ('S-C3','Office','2025-03-01T15:00:00',25.40,42.10,0);
END

IF OBJECT_ID('dbo.tb_demo_country', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tb_demo_country (
        id_country INT IDENTITY(1,1) PRIMARY KEY,
        iso_code CHAR(2) NOT NULL,
        country_name VARCHAR(80) NOT NULL,
        region VARCHAR(40) NOT NULL,
        population BIGINT NOT NULL,
        gdp_usd DECIMAL(18,2) NOT NULL
    );
    INSERT INTO dbo.tb_demo_country (iso_code, country_name, region, population, gdp_usd) VALUES
    ('US','United States','Americas',331000000,25000000000000.00),
    ('BR','Brazil','Americas',214000000,2100000000000.00),
    ('GB','United Kingdom','Europe',67000000,3100000000000.00),
    ('DE','Germany','Europe',83000000,4200000000000.00),
    ('JP','Japan','Asia',125000000,4900000000000.00),
    ('IN','India','Asia',1400000000,3700000000000.00),
    ('AU','Australia','Oceania',26000000,1700000000000.00),
    ('ZA','South Africa','Africa',60000000,400000000000.00);
END

IF OBJECT_ID('dbo.tb_demo_flight', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tb_demo_flight (
        id_flight INT IDENTITY(1,1) PRIMARY KEY,
        flight_number VARCHAR(10) NOT NULL,
        origin VARCHAR(5) NOT NULL,
        destination VARCHAR(5) NOT NULL,
        departure_at DATETIME2 NOT NULL,
        arrival_at DATETIME2 NOT NULL,
        status VARCHAR(20) NOT NULL,
        seats_available INT NOT NULL,
        fare_amount DECIMAL(18,2) NOT NULL
    );
    INSERT INTO dbo.tb_demo_flight (flight_number, origin, destination, departure_at, arrival_at, status, seats_available, fare_amount) VALUES
    ('QP100','GRU','JFK','2025-08-01T22:00:00','2025-08-02T07:30:00','Scheduled',24,1450.00),
    ('QP101','JFK','GRU','2025-08-03T20:00:00','2025-08-04T08:15:00','Scheduled',12,1520.50),
    ('QP200','GRU','LIS','2025-08-05T23:30:00','2025-08-06T12:00:00','Scheduled',40,980.00),
    ('QP201','LIS','GRU','2025-08-07T14:00:00','2025-08-07T20:45:00','Delayed',8,1010.00),
    ('QP300','CGH','BSB','2025-08-01T09:00:00','2025-08-01T10:40:00','Landed',0,420.00),
    ('QP301','BSB','CGH','2025-08-01T18:00:00','2025-08-01T19:45:00','Cancelled',60,410.00);
END

IF OBJECT_ID('dbo.tb_demo_transaction', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tb_demo_transaction (
        id_transaction INT IDENTITY(1,1) PRIMARY KEY,
        tx_code VARCHAR(30) NOT NULL,
        tx_type VARCHAR(20) NOT NULL,
        tx_at DATETIME2 NOT NULL,
        amount DECIMAL(18,2) NOT NULL,
        fee_pct DECIMAL(5,2) NOT NULL,
        currency VARCHAR(3) NOT NULL,
        is_reversed BIT NOT NULL DEFAULT(0),
        reference_note VARCHAR(200) NULL
    );
    INSERT INTO dbo.tb_demo_transaction (tx_code, tx_type, tx_at, amount, fee_pct, currency, is_reversed, reference_note) VALUES
    ('TX-1001','Credit','2025-01-05T10:15:00',1000.00,1.50,'USD',0,'Wire in'),
    ('TX-1002','Debit','2025-01-06T11:20:00',250.75,0.50,'USD',0,'Card payment'),
    ('TX-1003','Debit','2025-01-07T09:00:00',80.00,0.00,'USD',1,'Reversed charge'),
    ('TX-1004','Credit','2025-02-01T16:45:00',5000.00,2.00,'EUR',0,'FX deposit'),
    ('TX-1005','Debit','2025-02-14T13:10:00',199.99,1.00,'USD',0,'Subscription'),
    ('TX-1006','Credit','2025-03-01T08:05:00',750.25,1.25,'BRL',0,'Local transfer');
END
GO
IF OBJECT_ID('dbo.Sp_USA_President_List', 'P') IS NOT NULL DROP PROCEDURE dbo.Sp_USA_President_List;
GO
CREATE PROCEDURE dbo.Sp_USA_President_List
    @State VARCHAR(50) = NULL,
    @Start DATE = NULL,
    @End DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        p.term_number AS TermNumber,
        p.full_name AS FullName,
        p.party AS Party,
        p.birth_state AS BirthState,
        p.birth_date AS BirthDate,
        p.term_start AS TermStart,
        p.term_end AS TermEnd
    FROM dbo.tb_usa_president p
    WHERE (@State IS NULL OR LTRIM(RTRIM(@State)) = '' OR p.birth_state = @State)
      AND (@Start IS NULL OR p.term_start >= @Start)
      AND (@End IS NULL OR ISNULL(p.term_end, CAST('9999-12-31' AS DATE)) <= @End)
    ORDER BY p.term_number;
END
GO
IF OBJECT_ID('dbo.Sp_Demo_Customer_All', 'P') IS NOT NULL DROP PROCEDURE dbo.Sp_Demo_Customer_All;
GO
CREATE PROCEDURE dbo.Sp_Demo_Customer_All
AS
BEGIN
    SET NOCOUNT ON;
    SELECT id_customer AS Id, code AS Code, full_name AS FullName, city AS City, country AS Country,
           is_active AS IsActive, credit_limit AS CreditLimit, created_at AS CreatedAt
    FROM dbo.tb_demo_customer ORDER BY full_name;
END
GO
IF OBJECT_ID('dbo.Sp_Demo_Customer_ByName', 'P') IS NOT NULL DROP PROCEDURE dbo.Sp_Demo_Customer_ByName;
GO
CREATE PROCEDURE dbo.Sp_Demo_Customer_ByName
    @Name VARCHAR(120) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT id_customer AS Id, code AS Code, full_name AS FullName, city AS City, country AS Country, credit_limit AS CreditLimit
    FROM dbo.tb_demo_customer
    WHERE @Name IS NULL OR LTRIM(RTRIM(@Name)) = '' OR full_name LIKE '%' + @Name + '%'
    ORDER BY full_name;
END
GO
IF OBJECT_ID('dbo.Sp_Demo_Customer_ByCountry', 'P') IS NOT NULL DROP PROCEDURE dbo.Sp_Demo_Customer_ByCountry;
GO
CREATE PROCEDURE dbo.Sp_Demo_Customer_ByCountry
    @Country VARCHAR(60) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT code AS Code, full_name AS FullName, city AS City, country AS Country, is_active AS IsActive, credit_limit AS CreditLimit
    FROM dbo.tb_demo_customer
    WHERE @Country IS NULL OR LTRIM(RTRIM(@Country)) = '' OR country = @Country
    ORDER BY country, full_name;
END
GO
IF OBJECT_ID('dbo.Sp_Demo_Customer_ActiveOnly', 'P') IS NOT NULL DROP PROCEDURE dbo.Sp_Demo_Customer_ActiveOnly;
GO
CREATE PROCEDURE dbo.Sp_Demo_Customer_ActiveOnly
    @ActiveOnly BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    SELECT code AS Code, full_name AS FullName, is_active AS IsActive, credit_limit AS CreditLimit
    FROM dbo.tb_demo_customer
    WHERE @ActiveOnly = 0 OR is_active = 1
    ORDER BY full_name;
END
GO
IF OBJECT_ID('dbo.Sp_Demo_Customer_MinCredit', 'P') IS NOT NULL DROP PROCEDURE dbo.Sp_Demo_Customer_MinCredit;
GO
CREATE PROCEDURE dbo.Sp_Demo_Customer_MinCredit
    @MinCredit DECIMAL(18,2) = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT code AS Code, full_name AS FullName, credit_limit AS CreditLimit
    FROM dbo.tb_demo_customer
    WHERE credit_limit >= ISNULL(@MinCredit, 0)
    ORDER BY credit_limit DESC;
END
GO
IF OBJECT_ID('dbo.Sp_Demo_Product_List', 'P') IS NOT NULL DROP PROCEDURE dbo.Sp_Demo_Product_List;
GO
CREATE PROCEDURE dbo.Sp_Demo_Product_List
    @Category VARCHAR(50) = NULL,
    @IncludeDiscontinued BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT sku AS Sku, product_name AS ProductName, category AS Category, unit_price AS UnitPrice,
           stock_qty AS StockQty, discount_pct AS DiscountPct, is_discontinued AS IsDiscontinued
    FROM dbo.tb_demo_product
    WHERE (@Category IS NULL OR LTRIM(RTRIM(@Category)) = '' OR category = @Category)
      AND (@IncludeDiscontinued = 1 OR is_discontinued = 0)
    ORDER BY category, product_name;
END
GO
IF OBJECT_ID('dbo.Sp_Demo_Product_PriceRange', 'P') IS NOT NULL DROP PROCEDURE dbo.Sp_Demo_Product_PriceRange;
GO
CREATE PROCEDURE dbo.Sp_Demo_Product_PriceRange
    @MinPrice DECIMAL(18,2) = 0,
    @MaxPrice DECIMAL(18,2) = 999999
AS
BEGIN
    SET NOCOUNT ON;
    SELECT sku AS Sku, product_name AS ProductName, unit_price AS UnitPrice, stock_qty AS StockQty
    FROM dbo.tb_demo_product
    WHERE unit_price BETWEEN ISNULL(@MinPrice,0) AND ISNULL(@MaxPrice,999999)
    ORDER BY unit_price;
END
GO
IF OBJECT_ID('dbo.Sp_Demo_Order_ByDateRange', 'P') IS NOT NULL DROP PROCEDURE dbo.Sp_Demo_Order_ByDateRange;
GO
CREATE PROCEDURE dbo.Sp_Demo_Order_ByDateRange
    @Start DATE = NULL,
    @End DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT o.id_order AS OrderId, c.full_name AS CustomerName, o.order_date AS OrderDate, o.ship_date AS ShipDate,
           o.status AS Status, o.total_amount AS TotalAmount, o.tax_amount AS TaxAmount
    FROM dbo.tb_demo_order o
    INNER JOIN dbo.tb_demo_customer c ON c.id_customer = o.id_customer
    WHERE (@Start IS NULL OR o.order_date >= @Start)
      AND (@End IS NULL OR o.order_date <= @End)
    ORDER BY o.order_date DESC;
END
GO
IF OBJECT_ID('dbo.Sp_Demo_Order_ByStatus', 'P') IS NOT NULL DROP PROCEDURE dbo.Sp_Demo_Order_ByStatus;
GO
CREATE PROCEDURE dbo.Sp_Demo_Order_ByStatus
    @Status VARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT id_order AS OrderId, order_date AS OrderDate, status AS Status, total_amount AS TotalAmount
    FROM dbo.tb_demo_order
    WHERE @Status IS NULL OR LTRIM(RTRIM(@Status)) = '' OR status = @Status
    ORDER BY order_date DESC;
END
GO
IF OBJECT_ID('dbo.Sp_Demo_Order_Summary', 'P') IS NOT NULL DROP PROCEDURE dbo.Sp_Demo_Order_Summary;
GO
CREATE PROCEDURE dbo.Sp_Demo_Order_Summary
AS
BEGIN
    SET NOCOUNT ON;
    SELECT status AS Status, COUNT(*) AS OrderCount, SUM(total_amount) AS TotalAmount, AVG(total_amount) AS AvgAmount
    FROM dbo.tb_demo_order
    GROUP BY status
    ORDER BY status;
END
GO
IF OBJECT_ID('dbo.Sp_Demo_Employee_List', 'P') IS NOT NULL DROP PROCEDURE dbo.Sp_Demo_Employee_List;
GO
CREATE PROCEDURE dbo.Sp_Demo_Employee_List
    @Department VARCHAR(50) = NULL,
    @ManagersOnly BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT employee_code AS Code, full_name AS FullName, department AS Department, job_title AS JobTitle,
           hire_date AS HireDate, salary AS Salary, is_manager AS IsManager,
           work_start_time AS WorkStart, work_end_time AS WorkEnd
    FROM dbo.tb_demo_employee
    WHERE (@Department IS NULL OR LTRIM(RTRIM(@Department)) = '' OR department = @Department)
      AND (@ManagersOnly = 0 OR is_manager = 1)
    ORDER BY department, full_name;
END
GO
IF OBJECT_ID('dbo.Sp_Demo_Employee_ByHireDate', 'P') IS NOT NULL DROP PROCEDURE dbo.Sp_Demo_Employee_ByHireDate;
GO
CREATE PROCEDURE dbo.Sp_Demo_Employee_ByHireDate
    @HiredAfter DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT full_name AS FullName, department AS Department, hire_date AS HireDate, salary AS Salary
    FROM dbo.tb_demo_employee
    WHERE @HiredAfter IS NULL OR hire_date >= @HiredAfter
    ORDER BY hire_date;
END
GO
IF OBJECT_ID('dbo.Sp_Demo_Employee_Shift', 'P') IS NOT NULL DROP PROCEDURE dbo.Sp_Demo_Employee_Shift;
GO
CREATE PROCEDURE dbo.Sp_Demo_Employee_Shift
    @StartTime TIME = NULL,
    @EndTime TIME = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT full_name AS FullName, department AS Department, work_start_time AS WorkStart, work_end_time AS WorkEnd
    FROM dbo.tb_demo_employee
    WHERE (@StartTime IS NULL OR work_start_time >= @StartTime)
      AND (@EndTime IS NULL OR work_end_time <= @EndTime)
    ORDER BY work_start_time;
END
GO
IF OBJECT_ID('dbo.Sp_Demo_Employee_SalaryBand', 'P') IS NOT NULL DROP PROCEDURE dbo.Sp_Demo_Employee_SalaryBand;
GO
CREATE PROCEDURE dbo.Sp_Demo_Employee_SalaryBand
    @MinSalary DECIMAL(18,2) = 0,
    @MaxSalary DECIMAL(18,2) = 999999,
    @Name VARCHAR(120) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT full_name AS FullName, job_title AS JobTitle, salary AS Salary
    FROM dbo.tb_demo_employee
    WHERE salary BETWEEN ISNULL(@MinSalary,0) AND ISNULL(@MaxSalary,999999)
      AND (@Name IS NULL OR LTRIM(RTRIM(@Name)) = '' OR full_name LIKE '%' + @Name + '%')
    ORDER BY salary DESC;
END
GO
IF OBJECT_ID('dbo.Sp_Demo_Invoice_List', 'P') IS NOT NULL DROP PROCEDURE dbo.Sp_Demo_Invoice_List;
GO
CREATE PROCEDURE dbo.Sp_Demo_Invoice_List
    @Currency VARCHAR(3) = NULL,
    @PaidOnly BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT invoice_number AS InvoiceNumber, issue_date AS IssueDate, due_date AS DueDate, paid_at AS PaidAt,
           amount AS Amount, currency AS Currency, is_paid AS IsPaid
    FROM dbo.tb_demo_invoice
    WHERE (@Currency IS NULL OR LTRIM(RTRIM(@Currency)) = '' OR currency = @Currency)
      AND (@PaidOnly = 0 OR is_paid = 1)
    ORDER BY issue_date DESC;
END
GO
IF OBJECT_ID('dbo.Sp_Demo_Invoice_Overdue', 'P') IS NOT NULL DROP PROCEDURE dbo.Sp_Demo_Invoice_Overdue;
GO
CREATE PROCEDURE dbo.Sp_Demo_Invoice_Overdue
    @AsOf DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @d DATE = ISNULL(@AsOf, CAST(GETDATE() AS DATE));
    SELECT invoice_number AS InvoiceNumber, due_date AS DueDate, amount AS Amount, currency AS Currency,
           DATEDIFF(DAY, due_date, @d) AS DaysOverdue
    FROM dbo.tb_demo_invoice
    WHERE is_paid = 0 AND due_date < @d
    ORDER BY due_date;
END
GO
IF OBJECT_ID('dbo.Sp_Demo_Invoice_ByDateTimePaid', 'P') IS NOT NULL DROP PROCEDURE dbo.Sp_Demo_Invoice_ByDateTimePaid;
GO
CREATE PROCEDURE dbo.Sp_Demo_Invoice_ByDateTimePaid
    @PaidFrom DATETIME = NULL,
    @PaidTo DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT invoice_number AS InvoiceNumber, paid_at AS PaidAt, amount AS Amount, currency AS Currency
    FROM dbo.tb_demo_invoice
    WHERE is_paid = 1
      AND (@PaidFrom IS NULL OR paid_at >= @PaidFrom)
      AND (@PaidTo IS NULL OR paid_at <= @PaidTo)
    ORDER BY paid_at;
END
GO
IF OBJECT_ID('dbo.Sp_Demo_Event_List', 'P') IS NOT NULL DROP PROCEDURE dbo.Sp_Demo_Event_List;
GO
CREATE PROCEDURE dbo.Sp_Demo_Event_List
    @EventType VARCHAR(40) = NULL,
    @PublicOnly BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT event_name AS EventName, event_type AS EventType, event_date AS EventDate,
           start_time AS StartTime, end_time AS EndTime, location AS Location, capacity AS Capacity, is_public AS IsPublic
    FROM dbo.tb_demo_event
    WHERE (@EventType IS NULL OR LTRIM(RTRIM(@EventType)) = '' OR event_type = @EventType)
      AND (@PublicOnly = 0 OR is_public = 1)
    ORDER BY event_date;
END
GO
IF OBJECT_ID('dbo.Sp_Demo_Event_OnDate', 'P') IS NOT NULL DROP PROCEDURE dbo.Sp_Demo_Event_OnDate;
GO
CREATE PROCEDURE dbo.Sp_Demo_Event_OnDate
    @EventDate DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT event_name AS EventName, event_date AS EventDate, start_time AS StartTime, end_time AS EndTime, location AS Location
    FROM dbo.tb_demo_event
    WHERE @EventDate IS NULL OR event_date = @EventDate
    ORDER BY start_time;
END
GO
IF OBJECT_ID('dbo.Sp_Demo_Sensor_Readings', 'P') IS NOT NULL DROP PROCEDURE dbo.Sp_Demo_Sensor_Readings;
GO
CREATE PROCEDURE dbo.Sp_Demo_Sensor_Readings
    @Location VARCHAR(80) = NULL,
    @AlertsOnly BIT = 0,
    @From DATETIME = NULL,
    @To DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT sensor_code AS SensorCode, location AS Location, reading_at AS ReadingAt,
           temperature_c AS TemperatureC, humidity_pct AS HumidityPct, is_alert AS IsAlert
    FROM dbo.tb_demo_sensor
    WHERE (@Location IS NULL OR LTRIM(RTRIM(@Location)) = '' OR location = @Location)
      AND (@AlertsOnly = 0 OR is_alert = 1)
      AND (@From IS NULL OR reading_at >= @From)
      AND (@To IS NULL OR reading_at <= @To)
    ORDER BY reading_at;
END
GO
IF OBJECT_ID('dbo.Sp_Demo_Sensor_ByTemp', 'P') IS NOT NULL DROP PROCEDURE dbo.Sp_Demo_Sensor_ByTemp;
GO
CREATE PROCEDURE dbo.Sp_Demo_Sensor_ByTemp
    @MinTemp DECIMAL(6,2) = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT sensor_code AS SensorCode, location AS Location, reading_at AS ReadingAt, temperature_c AS TemperatureC
    FROM dbo.tb_demo_sensor
    WHERE temperature_c >= ISNULL(@MinTemp, 0)
    ORDER BY temperature_c DESC;
END
GO
IF OBJECT_ID('dbo.Sp_Demo_Country_List', 'P') IS NOT NULL DROP PROCEDURE dbo.Sp_Demo_Country_List;
GO
CREATE PROCEDURE dbo.Sp_Demo_Country_List
    @Region VARCHAR(40) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT iso_code AS IsoCode, country_name AS CountryName, region AS Region, population AS Population, gdp_usd AS GdpUsd
    FROM dbo.tb_demo_country
    WHERE @Region IS NULL OR LTRIM(RTRIM(@Region)) = '' OR region = @Region
    ORDER BY population DESC;
END
GO
IF OBJECT_ID('dbo.Sp_Demo_Country_Search', 'P') IS NOT NULL DROP PROCEDURE dbo.Sp_Demo_Country_Search;
GO
CREATE PROCEDURE dbo.Sp_Demo_Country_Search
    @Name VARCHAR(80) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT iso_code AS IsoCode, country_name AS CountryName, region AS Region
    FROM dbo.tb_demo_country
    WHERE @Name IS NULL OR LTRIM(RTRIM(@Name)) = '' OR country_name LIKE '%' + @Name + '%'
    ORDER BY country_name;
END
GO
IF OBJECT_ID('dbo.Sp_Demo_Flight_List', 'P') IS NOT NULL DROP PROCEDURE dbo.Sp_Demo_Flight_List;
GO
CREATE PROCEDURE dbo.Sp_Demo_Flight_List
    @Status VARCHAR(20) = NULL,
    @Origin VARCHAR(5) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT flight_number AS FlightNumber, origin AS Origin, destination AS Destination,
           departure_at AS DepartureAt, arrival_at AS ArrivalAt, status AS Status,
           seats_available AS SeatsAvailable, fare_amount AS FareAmount
    FROM dbo.tb_demo_flight
    WHERE (@Status IS NULL OR LTRIM(RTRIM(@Status)) = '' OR status = @Status)
      AND (@Origin IS NULL OR LTRIM(RTRIM(@Origin)) = '' OR origin = @Origin)
    ORDER BY departure_at;
END
GO
IF OBJECT_ID('dbo.Sp_Demo_Flight_ByDepartureWindow', 'P') IS NOT NULL DROP PROCEDURE dbo.Sp_Demo_Flight_ByDepartureWindow;
GO
CREATE PROCEDURE dbo.Sp_Demo_Flight_ByDepartureWindow
    @From DATETIME = NULL,
    @To DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT flight_number AS FlightNumber, origin AS Origin, destination AS Destination, departure_at AS DepartureAt, fare_amount AS FareAmount
    FROM dbo.tb_demo_flight
    WHERE (@From IS NULL OR departure_at >= @From)
      AND (@To IS NULL OR departure_at <= @To)
    ORDER BY departure_at;
END
GO
IF OBJECT_ID('dbo.Sp_Demo_Transaction_List', 'P') IS NOT NULL DROP PROCEDURE dbo.Sp_Demo_Transaction_List;
GO
CREATE PROCEDURE dbo.Sp_Demo_Transaction_List
    @TxType VARCHAR(20) = NULL,
    @IncludeReversed BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT tx_code AS TxCode, tx_type AS TxType, tx_at AS TxAt, amount AS Amount, fee_pct AS FeePct,
           currency AS Currency, is_reversed AS IsReversed, reference_note AS ReferenceNote
    FROM dbo.tb_demo_transaction
    WHERE (@TxType IS NULL OR LTRIM(RTRIM(@TxType)) = '' OR tx_type = @TxType)
      AND (@IncludeReversed = 1 OR is_reversed = 0)
    ORDER BY tx_at DESC;
END
GO
IF OBJECT_ID('dbo.Sp_Demo_Transaction_AmountRange', 'P') IS NOT NULL DROP PROCEDURE dbo.Sp_Demo_Transaction_AmountRange;
GO
CREATE PROCEDURE dbo.Sp_Demo_Transaction_AmountRange
    @MinAmount DECIMAL(18,2) = 0,
    @MaxAmount DECIMAL(18,2) = 999999,
    @Currency VARCHAR(3) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT tx_code AS TxCode, tx_type AS TxType, amount AS Amount, currency AS Currency, tx_at AS TxAt
    FROM dbo.tb_demo_transaction
    WHERE amount BETWEEN ISNULL(@MinAmount,0) AND ISNULL(@MaxAmount,999999)
      AND (@Currency IS NULL OR LTRIM(RTRIM(@Currency)) = '' OR currency = @Currency)
    ORDER BY amount DESC;
END
GO
IF OBJECT_ID('dbo.Sp_Demo_KitchenSink', 'P') IS NOT NULL DROP PROCEDURE dbo.Sp_Demo_KitchenSink;
GO
CREATE PROCEDURE dbo.Sp_Demo_KitchenSink
    @Name VARCHAR(120) = NULL,
    @MinCredit DECIMAL(18,2) = 0,
    @CreatedAfter DATE = NULL,
    @WorkStart TIME = NULL,
    @ChangedAfter DATETIME = NULL,
    @ActiveOnly BIT = 1,
    @Country VARCHAR(60) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.code AS Code, c.full_name AS FullName, c.country AS Country, c.is_active AS IsActive,
           c.credit_limit AS CreditLimit, c.created_at AS CreatedAt
    FROM dbo.tb_demo_customer c
    WHERE (@Name IS NULL OR LTRIM(RTRIM(@Name)) = '' OR c.full_name LIKE '%' + @Name + '%')
      AND c.credit_limit >= ISNULL(@MinCredit, 0)
      AND (@CreatedAfter IS NULL OR CAST(c.created_at AS DATE) >= @CreatedAfter)
      AND (@ActiveOnly = 0 OR c.is_active = 1)
      AND (@Country IS NULL OR LTRIM(RTRIM(@Country)) = '' OR c.country = @Country)
      AND (@WorkStart IS NULL OR 1 = 1)
      AND (@ChangedAfter IS NULL OR 1 = 1)
    ORDER BY c.full_name;
END
GO
IF OBJECT_ID('dbo.Sp_Demo_Alignment_Showcase', 'P') IS NOT NULL DROP PROCEDURE dbo.Sp_Demo_Alignment_Showcase;
GO
CREATE PROCEDURE dbo.Sp_Demo_Alignment_Showcase
AS
BEGIN
    SET NOCOUNT ON;
    SELECT product_name AS LeftText, category AS CenterText, unit_price AS RightNumber, discount_pct AS RightPct
    FROM dbo.tb_demo_product
    ORDER BY product_name;
END
GO
IF OBJECT_ID('dbo.Sp_Demo_Hidden_Columns', 'P') IS NOT NULL DROP PROCEDURE dbo.Sp_Demo_Hidden_Columns;
GO
CREATE PROCEDURE dbo.Sp_Demo_Hidden_Columns
AS
BEGIN
    SET NOCOUNT ON;
    SELECT id_product AS IdProduct, sku AS Sku, product_name AS ProductName, unit_price AS UnitPrice, stock_qty AS StockQty
    FROM dbo.tb_demo_product
    ORDER BY product_name;
END
GO
IF OBJECT_ID('dbo.Sp_Demo_Empty_Result', 'P') IS NOT NULL DROP PROCEDURE dbo.Sp_Demo_Empty_Result;
GO
CREATE PROCEDURE dbo.Sp_Demo_Empty_Result
    @ImpossibleCode VARCHAR(20) = '___NONE___'
AS
BEGIN
    SET NOCOUNT ON;
    SELECT code AS Code, full_name AS FullName
    FROM dbo.tb_demo_customer
    WHERE code = @ImpossibleCode;
END
GO
IF OBJECT_ID('dbo.Sp_Demo_Boolean_Only', 'P') IS NOT NULL DROP PROCEDURE dbo.Sp_Demo_Boolean_Only;
GO
CREATE PROCEDURE dbo.Sp_Demo_Boolean_Only
    @ActiveOnly BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    SELECT full_name AS FullName, is_active AS IsActive FROM dbo.tb_demo_customer
    WHERE @ActiveOnly = 0 OR is_active = 1 ORDER BY full_name;
END
GO
IF OBJECT_ID('dbo.Sp_Demo_Time_Only', 'P') IS NOT NULL DROP PROCEDURE dbo.Sp_Demo_Time_Only;
GO
CREATE PROCEDURE dbo.Sp_Demo_Time_Only
    @After TIME = '12:00'
AS
BEGIN
    SET NOCOUNT ON;
    SELECT full_name AS FullName, work_start_time AS WorkStart, work_end_time AS WorkEnd
    FROM dbo.tb_demo_employee
    WHERE work_start_time >= ISNULL(@After, '00:00')
    ORDER BY work_start_time;
END
GO
-- Large result set for UI scrollbar / grid performance testing (default 1,000 rows).
IF OBJECT_ID('dbo.Sp_Demo_Large_Result', 'P') IS NOT NULL DROP PROCEDURE dbo.Sp_Demo_Large_Result;
GO
CREATE PROCEDURE dbo.Sp_Demo_Large_Result
    @RowCount INT = 1000
AS
BEGIN
    SET NOCOUNT ON;

    IF @RowCount IS NULL OR @RowCount < 1
        SET @RowCount = 1000;
    IF @RowCount > 10000
        SET @RowCount = 10000;

    ;WITH n AS (
        SELECT 1 AS n
        UNION ALL
        SELECT n + 1 FROM n WHERE n < @RowCount
    )
    SELECT
        n AS RowId,
        'ITEM-' + RIGHT('0000' + CAST(n AS VARCHAR(10)), 4) AS ItemCode,
        'Sample product description for row ' + CAST(n AS VARCHAR(10))
            + ' — used to exercise vertical scrolling in the results grid.' AS Description,
        CAST((n % 12) + 1 AS INT) AS CategoryId,
        CAST(10.00 + (n % 250) * 1.37 AS DECIMAL(18, 2)) AS Amount,
        DATEADD(DAY, -(n % 730), CAST(SYSUTCDATETIME() AS DATE)) AS OrderDate,
        CASE WHEN n % 3 = 0 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsActive
    FROM n
    ORDER BY n
    OPTION (MAXRECURSION 0);
END
GO
-- ============================================================
-- Server-side pagination demos (QueryPlus contract):
--   @PageNumber BIGINT = 1
--   @PageSize BIGINT = 50
--   @TotalRecords BIGINT OUTPUT
-- ============================================================

IF OBJECT_ID('dbo.Sp_Demo_Numbers_Paged', 'P') IS NOT NULL DROP PROCEDURE dbo.Sp_Demo_Numbers_Paged;
GO
CREATE PROCEDURE dbo.Sp_Demo_Numbers_Paged
    @MaxNumber INT = 5000,
    @PageNumber BIGINT = 1,
    @PageSize BIGINT = 50,
    @TotalRecords BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF @MaxNumber IS NULL OR @MaxNumber < 1 SET @MaxNumber = 5000;
    IF @MaxNumber > 100000 SET @MaxNumber = 100000;
    IF @PageNumber IS NULL OR @PageNumber < 1 SET @PageNumber = 1;
    IF @PageSize IS NULL OR @PageSize < 1 SET @PageSize = 50;
    IF @PageSize > 999999999 SET @PageSize = 999999999;

    SET @TotalRecords = @MaxNumber;

    DECLARE @Offset BIGINT = (@PageNumber - 1) * @PageSize;

    ;WITH n AS (
        SELECT 1 AS n
        UNION ALL
        SELECT n + 1 FROM n WHERE n < @MaxNumber
    )
    SELECT
        n AS Number,
        'NUM-' + RIGHT('000000' + CAST(n AS VARCHAR(10)), 6) AS Code,
        'Generated row ' + CAST(n AS VARCHAR(10)) AS Label
    FROM n
    ORDER BY n
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
    OPTION (MAXRECURSION 0);
END
GO

IF OBJECT_ID('dbo.Sp_Demo_Large_Result_Paged', 'P') IS NOT NULL DROP PROCEDURE dbo.Sp_Demo_Large_Result_Paged;
GO
CREATE PROCEDURE dbo.Sp_Demo_Large_Result_Paged
    @RowCount INT = 5000,
    @PageNumber BIGINT = 1,
    @PageSize BIGINT = 50,
    @TotalRecords BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF @RowCount IS NULL OR @RowCount < 1 SET @RowCount = 5000;
    IF @RowCount > 50000 SET @RowCount = 50000;
    IF @PageNumber IS NULL OR @PageNumber < 1 SET @PageNumber = 1;
    IF @PageSize IS NULL OR @PageSize < 1 SET @PageSize = 50;
    IF @PageSize > 999999999 SET @PageSize = 999999999;

    SET @TotalRecords = @RowCount;

    DECLARE @Offset BIGINT = (@PageNumber - 1) * @PageSize;

    ;WITH n AS (
        SELECT 1 AS n
        UNION ALL
        SELECT n + 1 FROM n WHERE n < @RowCount
    )
    SELECT
        n AS RowId,
        'ITEM-' + RIGHT('0000' + CAST(n AS VARCHAR(10)), 4) AS ItemCode,
        'Sample product description for row ' + CAST(n AS VARCHAR(10))
            + ' — server-side paginated large result.' AS Description,
        CAST((n % 12) + 1 AS INT) AS CategoryId,
        CAST(10.00 + (n % 250) * 1.37 AS DECIMAL(18, 2)) AS Amount,
        DATEADD(DAY, -(n % 730), CAST(SYSUTCDATETIME() AS DATE)) AS OrderDate,
        CASE WHEN n % 3 = 0 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsActive
    FROM n
    ORDER BY n
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
    OPTION (MAXRECURSION 0);
END
GO

IF OBJECT_ID('dbo.Sp_USA_President_List_Paged', 'P') IS NOT NULL DROP PROCEDURE dbo.Sp_USA_President_List_Paged;
GO
CREATE PROCEDURE dbo.Sp_USA_President_List_Paged
    @State VARCHAR(50) = NULL,
    @Start DATE = NULL,
    @End DATE = NULL,
    @PageNumber BIGINT = 1,
    @PageSize BIGINT = 50,
    @TotalRecords BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF @PageNumber IS NULL OR @PageNumber < 1 SET @PageNumber = 1;
    IF @PageSize IS NULL OR @PageSize < 1 SET @PageSize = 50;
    IF @PageSize > 999999999 SET @PageSize = 999999999;

    ;WITH filtered AS (
        SELECT
            term_number AS TermNumber,
            full_name AS FullName,
            party AS Party,
            birth_state AS BirthState,
            birth_date AS BirthDate,
            term_start AS TermStart,
            term_end AS TermEnd
        FROM dbo.tb_usa_president
        WHERE (@State IS NULL OR @State = '' OR birth_state = @State)
          AND (@Start IS NULL OR term_start >= @Start)
          AND (@End IS NULL OR term_start <= @End)
    )
    SELECT @TotalRecords = COUNT_BIG(*) FROM filtered;

    DECLARE @Offset BIGINT = (@PageNumber - 1) * @PageSize;

    ;WITH filtered AS (
        SELECT
            term_number AS TermNumber,
            full_name AS FullName,
            party AS Party,
            birth_state AS BirthState,
            birth_date AS BirthDate,
            term_start AS TermStart,
            term_end AS TermEnd
        FROM dbo.tb_usa_president
        WHERE (@State IS NULL OR @State = '' OR birth_state = @State)
          AND (@Start IS NULL OR term_start >= @Start)
          AND (@End IS NULL OR term_start <= @End)
    )
    SELECT TermNumber, FullName, Party, BirthState, BirthDate, TermStart, TermEnd
    FROM filtered
    ORDER BY TermNumber
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
END
