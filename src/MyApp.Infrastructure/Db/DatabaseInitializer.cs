using Microsoft.EntityFrameworkCore;
using MyApp.Application.Abstractions;
using MyApp.Domain.Entities;

namespace MyApp.Infrastructure.Db;

public sealed class DatabaseInitializer(AppDbContext db) : IDatabaseInitializer
{
    public async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS professions (
                id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
                profession varchar(40) NOT NULL
            )
            """,
            cancellationToken);
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS app_users (
                id uuid PRIMARY KEY,
                first_name varchar(100) NOT NULL,
                middle_name varchar(100) NOT NULL,
                last_name varchar(100) NOT NULL,
                user_name varchar(50) NOT NULL UNIQUE,
                email varchar(100) NOT NULL UNIQUE,
                password_hash text NOT NULL,
                position uuid NOT NULL,
                role varchar(30) NOT NULL,
                created_at timestamptz NOT NULL DEFAULT NOW(),
                CONSTRAINT fk_app_users_professions_position
                    FOREIGN KEY (position) REFERENCES professions(id)
                    ON DELETE RESTRICT
            )
            """,
            cancellationToken);
        await db.Database.ExecuteSqlRawAsync(
            """
            DO $$
            DECLARE
                position_data_type text;
            BEGIN
                SELECT data_type
                INTO position_data_type
                FROM information_schema.columns
                WHERE table_schema = 'public'
                  AND table_name = 'app_users'
                  AND column_name = 'position';

                IF position_data_type IN ('character varying', 'text') THEN
                    INSERT INTO professions (profession)
                    SELECT DISTINCT LEFT(BTRIM(users.position), 40)
                    FROM app_users AS users
                    WHERE BTRIM(users.position) <> ''
                      AND NOT EXISTS (
                          SELECT 1
                          FROM professions AS professions
                          WHERE LOWER(professions.profession) =
                                LOWER(LEFT(BTRIM(users.position), 40))
                      );

                    INSERT INTO professions (profession)
                    SELECT 'Не указана'
                    WHERE EXISTS (
                        SELECT 1 FROM app_users WHERE BTRIM(position) = ''
                    )
                      AND NOT EXISTS (
                        SELECT 1 FROM professions
                        WHERE LOWER(profession) = LOWER('Не указана')
                    );

                    ALTER TABLE app_users ADD COLUMN position_id uuid;

                    UPDATE app_users AS users
                    SET position_id = professions.id
                    FROM professions AS professions
                    WHERE LOWER(professions.profession) =
                          LOWER(LEFT(BTRIM(users.position), 40));

                    UPDATE app_users AS users
                    SET position_id = professions.id
                    FROM professions AS professions
                    WHERE users.position_id IS NULL
                      AND LOWER(professions.profession) =
                          LOWER('Не указана');

                    ALTER TABLE app_users
                        ALTER COLUMN position_id SET NOT NULL;
                    ALTER TABLE app_users DROP COLUMN position;
                    ALTER TABLE app_users
                        RENAME COLUMN position_id TO position;
                END IF;
            END
            $$;
            """,
            cancellationToken);
        await db.Database.ExecuteSqlRawAsync(
            """
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1
                    FROM pg_constraint
                    WHERE conname = 'fk_app_users_professions_position'
                ) THEN
                    ALTER TABLE app_users
                    ADD CONSTRAINT fk_app_users_professions_position
                    FOREIGN KEY (position)
                    REFERENCES professions(id)
                    ON DELETE RESTRICT;
                END IF;
            END
            $$;
            """,
            cancellationToken);
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS component_requirements (
                id uuid PRIMARY KEY,
                created_at timestamptz NOT NULL DEFAULT NOW(),
                created_by uuid NOT NULL,
                author_name varchar(300) NOT NULL,
                issuer_name varchar(300) NOT NULL,
                vehicle_number varchar(100) NOT NULL,
                source_table varchar(20) NOT NULL,
                pdf_file_name varchar(200) NOT NULL,
                pdf_content bytea NOT NULL,
                CONSTRAINT fk_component_requirements_created_by
                    FOREIGN KEY (created_by) REFERENCES app_users(id)
                    ON DELETE RESTRICT
            );

            CREATE TABLE IF NOT EXISTS component_requirement_items (
                requirement_id uuid NOT NULL,
                position integer NOT NULL,
                name text NOT NULL,
                unit varchar(100) NOT NULL,
                quantity numeric NOT NULL,
                PRIMARY KEY (requirement_id, position),
                CONSTRAINT fk_component_requirement_items_requirement
                    FOREIGN KEY (requirement_id)
                    REFERENCES component_requirements(id)
                    ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS ix_component_requirements_created_at
                ON component_requirements (created_at DESC);

            ALTER TABLE component_requirements
                ADD COLUMN IF NOT EXISTS issuer_name varchar(300) NOT NULL DEFAULT '',
                ADD COLUMN IF NOT EXISTS source_table varchar(20) NOT NULL DEFAULT '',
                ADD COLUMN IF NOT EXISTS pdf_file_name varchar(200) NOT NULL DEFAULT '',
                ADD COLUMN IF NOT EXISTS pdf_content bytea;

            UPDATE component_requirements AS requirements
            SET source_table = CASE
                WHEN LOWER(employees."Profession") = LOWER('Кладовщик')
                    THEN 'full_ost'
                WHEN LOWER(employees."Profession") = LOWER('Старший механик')
                    THEN 'meh_ost'
                ELSE requirements.source_table
            END
            FROM employees
            WHERE requirements.source_table = ''
              AND REGEXP_REPLACE(
                    BTRIM(employees."FullName"),
                    '\s+',
                    ' ',
                    'g') = REGEXP_REPLACE(
                        BTRIM(requirements.issuer_name),
                        '\s+',
                        ' ',
                        'g');
            """,
            cancellationToken);
        var administratorProfession = await db.Professions.FirstOrDefaultAsync(
            profession => profession.Name == "Администратор",
            cancellationToken);
        if (administratorProfession is null)
        {
            administratorProfession = new Profession
            {
                Id = Guid.NewGuid(),
                Name = "Администратор"
            };
            db.Professions.Add(administratorProfession);
            await db.SaveChangesAsync(cancellationToken);
        }

        if (await db.Users.AnyAsync(
            user => user.UserName == "boora",
            cancellationToken))
        {
            return;
        }

        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Администратор",
            MiddleName = string.Empty,
            LastName = "Системы",
            UserName = "boora",
            Email = "boora@local",
            PasswordHash =
                "pbkdf2-sha256$100000$fwsW/+p0gAm3idMJZNn4Jw==$" +
                "RywEjkyVe79il+wrlDQ2SKatTxMl4GLyzef57VQDf54=",
            PositionId = administratorProfession.Id,
            Role = "administrator",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}
