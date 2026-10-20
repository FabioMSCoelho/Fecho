CREATE DATABASE fecho
CHARACTER SET utf8mb4
COLLATE utf8mb4_unicode_ci;

USE fecho;

-- =========================================
-- TABELA DE USUÁRIOS
-- =========================================

CREATE TABLE usuarios (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nome VARCHAR(100) NOT NULL,
    email VARCHAR(150) NOT NULL UNIQUE,
    senha VARCHAR(255) NOT NULL,
    telefone VARCHAR(20) NOT NULL,
    foto VARCHAR(255),
    cidade VARCHAR(100),
    descricao VARCHAR(500),
    data_cadastro DATETIME DEFAULT CURRENT_TIMESTAMP,
    ativo BOOLEAN DEFAULT TRUE
);

-- =========================================
-- TABELA DE CATEGORIAS
-- =========================================

CREATE TABLE categorias (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nome VARCHAR(50) NOT NULL UNIQUE,
    descricao VARCHAR(255)
);

-- =========================================
-- TABELA DE OBJETOS
-- =========================================

CREATE TABLE objetos (
    id INT AUTO_INCREMENT PRIMARY KEY,
    usuario_id INT NOT NULL,
    categoria_id INT NOT NULL,

    nome VARCHAR(100) NOT NULL,
    descricao TEXT,
    estado_conservacao ENUM(
        'NOVO',
        'MUITO_BOM',
        'BOM',
        'REGULAR',
        'RUIM'
    ) NOT NULL,

    valor_estimado DECIMAL(10,2),

    imagem VARCHAR(255),

    status ENUM(
        'DISPONIVEL',
        'EM_NEGOCIACAO',
        'TROCADO',
        'INATIVO'
    ) DEFAULT 'DISPONIVEL',

    data_cadastro DATETIME DEFAULT CURRENT_TIMESTAMP,

    FOREIGN KEY (usuario_id)
        REFERENCES usuarios(id),

    FOREIGN KEY (categoria_id)
        REFERENCES categorias(id)
);

-- =========================================
-- TABELA DE DESEJOS
-- O que o usuário procura
-- =========================================

CREATE TABLE desejos (
    id INT AUTO_INCREMENT PRIMARY KEY,
    usuario_id INT NOT NULL,
    categoria_id INT,
    descricao VARCHAR(255) NOT NULL,

    FOREIGN KEY (usuario_id)
        REFERENCES usuarios(id)
        ON DELETE CASCADE,

    FOREIGN KEY (categoria_id)
        REFERENCES categorias(id)
);

-- =========================================
-- TABELA DE INTERESSES
-- LIKE / DISLIKE
-- =========================================

CREATE TABLE interesses (
    id INT AUTO_INCREMENT PRIMARY KEY,

    usuario_id INT NOT NULL,
    objeto_id INT NOT NULL,

    tipo ENUM('LIKE', 'DISLIKE') NOT NULL,

    data_interesse DATETIME DEFAULT CURRENT_TIMESTAMP,

    FOREIGN KEY (usuario_id)
        REFERENCES usuarios(id)
        ON DELETE CASCADE,

    FOREIGN KEY (objeto_id)
        REFERENCES objetos(id)
        ON DELETE CASCADE,

    UNIQUE (usuario_id, objeto_id)
);

-- =========================================
-- TABELA DE MATCHES
-- =========================================

CREATE TABLE matches (
    id INT AUTO_INCREMENT PRIMARY KEY,

    usuario1_id INT NOT NULL,
    usuario2_id INT NOT NULL,

    objeto1_id INT NOT NULL,
    objeto2_id INT NOT NULL,

    data_match DATETIME DEFAULT CURRENT_TIMESTAMP,

    status ENUM(
        'ATIVO',
        'CANCELADO',
        'CONCLUIDO'
    ) DEFAULT 'ATIVO',

    FOREIGN KEY (usuario1_id)
        REFERENCES usuarios(id),

    FOREIGN KEY (usuario2_id)
        REFERENCES usuarios(id),

    FOREIGN KEY (objeto1_id)
        REFERENCES objetos(id),

    FOREIGN KEY (objeto2_id)
        REFERENCES objetos(id)
);

-- =========================================
-- TABELA DE TROCAS
-- =========================================

CREATE TABLE trocas (
    id INT AUTO_INCREMENT PRIMARY KEY,

    match_id INT NOT NULL UNIQUE,

    data_conclusao DATETIME,

    observacao VARCHAR(500),

    FOREIGN KEY (match_id)
        REFERENCES matches(id)
);
