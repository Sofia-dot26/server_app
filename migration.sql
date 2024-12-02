CREATE TABLE Users (
    id SERIAL PRIMARY KEY,
    login VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    role VARCHAR(50) NOT NULL,
    regdate TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

INSERT INTO Users (id, login, password_hash, role) VALUES
(1, 'admin', MD5('password'), 'admin');

CREATE TABLE Materials (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    quantity INT NOT NULL,
    unit VARCHAR(50) NOT NULL
);

CREATE TABLE Suppliers (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    contact_info VARCHAR(255)
);

CREATE TABLE Supplies (
    id SERIAL PRIMARY KEY,
    material_id INT NOT NULL REFERENCES Materials(id),
    supplier_id INT NOT NULL REFERENCES Suppliers(id),
    quantity INT NOT NULL,
    date TIMESTAMP NOT NULL
);

CREATE TABLE Equipment (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    description TEXT
);

CREATE TABLE SpentMaterials (
    id SERIAL PRIMARY KEY,
    material_id INT NOT NULL REFERENCES Materials(id),
    quantity INT NOT NULL,
    date TIMESTAMP NOT NULL
);

CREATE TABLE Reports (
    id SERIAL PRIMARY KEY,
    report_type VARCHAR(255) NOT NULL,
    period_start DATE,
    period_end DATE,
    content TEXT NOT NULL
);

CREATE TABLE Sessions (
    id SERIAL PRIMARY KEY,
    user_id INT NOT NULL REFERENCES Users(id),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMP NOT NULL,
    ip VARCHAR(50)
);