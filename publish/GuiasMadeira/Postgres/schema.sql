-- Schema PostgreSQL para o software Guias Madeira.
-- Equivalente ao DB_guias.accdb (ver ANALISE_ACCESS.md), sem a tabela "Vias"
-- (era só um truque de agrupamento do relatório Access; passou a enum ViaImpressao em código).

CREATE TABLE IF NOT EXISTS proprietarios (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nome TEXT NOT NULL,
    distrito TEXT NULL,
    concelho TEXT NULL,
    freguesia TEXT NULL,
    codigo_prop TEXT NULL,
    parcela TEXT NULL
);

CREATE TABLE IF NOT EXISTS destinatarios (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nome TEXT NOT NULL,
    nif TEXT NULL,
    morada TEXT NULL,
    concelho TEXT NULL
);

CREATE TABLE IF NOT EXISTS rolarias (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    tipo TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS codigos_barras (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    codigo TEXT NOT NULL,
    numero_certificado TEXT NULL,
    numero_ugf TEXT NULL
);

CREATE TABLE IF NOT EXISTS guias (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    destinatario_id INTEGER NULL REFERENCES destinatarios (id) ON DELETE RESTRICT,
    proprietario_id INTEGER NULL REFERENCES proprietarios (id) ON DELETE RESTRICT,
    codigo_barra_id INTEGER NULL REFERENCES codigos_barras (id) ON DELETE RESTRICT,
    rolaria_id INTEGER NULL REFERENCES rolarias (id) ON DELETE RESTRICT,
    fornecedor TEXT NULL,
    criado_em TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_guias_destinatario ON guias (destinatario_id);
CREATE INDEX IF NOT EXISTS ix_guias_proprietario ON guias (proprietario_id);
CREATE INDEX IF NOT EXISTS ix_guias_codigo_barra ON guias (codigo_barra_id);
CREATE INDEX IF NOT EXISTS ix_guias_rolaria ON guias (rolaria_id);

-- Limite de toneladas por código UGF: cada UGF tem um certificado (limite) em toneladas,
-- e o consumo desse limite é registado por entrada (importada do ficheiro de entregas da fábrica),
-- não por guia. O código UGF compara-se por texto com codigos_barras.numero_ugf (sem FK,
-- para não alterar a tabela existente).
CREATE TABLE IF NOT EXISTS ugfs (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    codigo TEXT NOT NULL UNIQUE,
    toneladas_certificado NUMERIC(12,3) NOT NULL
);

CREATE TABLE IF NOT EXISTS ugf_entradas (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    ugf_id INTEGER NOT NULL REFERENCES ugfs (id) ON DELETE CASCADE,
    toneladas NUMERIC(12,3) NOT NULL,
    origem TEXT NULL,
    criado_em TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_ugf_entradas_ugf ON ugf_entradas (ugf_id);
