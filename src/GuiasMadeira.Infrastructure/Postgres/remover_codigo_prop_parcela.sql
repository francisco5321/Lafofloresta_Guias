-- Remove os campos "código de propriedade" e "parcela" de proprietarios, que deixaram de ser
-- usados no formulário. Corre isto uma vez contra a base de dados de produção (já não é preciso
-- correr de novo o schema.sql, que só cria tabelas em falta e não altera as existentes).

ALTER TABLE proprietarios DROP COLUMN IF EXISTS codigo_prop;
ALTER TABLE proprietarios DROP COLUMN IF EXISTS parcela;
