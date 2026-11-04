-- Exclusivo do Compose local: o usuário MYSQL_USER é criado pelo entrypoint.
-- Não concedemos DDL nem acesso a outras bases ao usuário da aplicação.
-- MYSQL_DATABASE não é usado: fecho.sql cria o banco original sem alterações.
GRANT SELECT, INSERT, UPDATE, DELETE ON fecho.* TO 'fecho'@'%';
