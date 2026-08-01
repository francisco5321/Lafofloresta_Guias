-- Provisiona o role "guias_app" usado pela connection string da aplicação, com o mínimo de
-- privilégios necessário para o Desktop funcionar: sem CREATEDB, sem CREATEROLE, sem SUPERUSER,
-- sem acesso a outras bases de dados no mesmo servidor. Se a credencial deste role for exposta
-- (ex. cópia do appsettings.json de um posto de trabalho), o estrago possível fica limitado a
-- fazer CRUD nas tabelas de negócio — não dá para apagar a base de dados, criar outros roles,
-- nem tocar noutras bases no mesmo servidor Postgres.
--
-- Corre isto uma vez no servidor de produção (ligado como um utilizador com privilégios de
-- administração, ex. "postgres"), depois de aplicar o schema.sql. É seguro voltar a correr
-- (idempotente).

DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'guias_app') THEN
        CREATE ROLE guias_app LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION;
    ELSE
        ALTER ROLE guias_app NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION;
    END IF;
END
$$;

-- Definir/rodar a password separadamente (não fica neste ficheiro nem em nenhum ficheiro
-- versionado):
--   ALTER ROLE guias_app WITH PASSWORD 'a-password-real-aqui';

GRANT CONNECT ON DATABASE guias_madeira TO guias_app;
GRANT USAGE ON SCHEMA public TO guias_app;
REVOKE CREATE ON SCHEMA public FROM guias_app;

GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO guias_app;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO guias_app;

-- Garante que tabelas/sequências criadas no futuro (novas migrações) já ficam com os mesmos
-- privilégios, sem ser preciso repetir os GRANTs acima manualmente.
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO guias_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO guias_app;

-- Rede: além dos GRANTs acima, restringe também a nível de rede no pg_hba.conf do servidor, para
-- que esta credencial não sirva de nada fora da VPN mesmo que vaze:
--   hostssl  guias_madeira  guias_app  <intervalo-de-IPs-da-VPN>/xx  scram-sha-256
-- e recarrega a configuração (SELECT pg_reload_conf(); ou reinicia o serviço).
