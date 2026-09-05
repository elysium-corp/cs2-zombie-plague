CREATE SCHEMA IF NOT EXISTS supply_box;
CREATE TABLE IF NOT EXISTS supply_box.configuration (
    id integer PRIMARY KEY DEFAULT 1 CHECK (id = 1),
    version bigint NOT NULL DEFAULT 1 CHECK (version > 0),
    exported_version bigint NOT NULL DEFAULT 0,
    legacy_imported boolean NOT NULL DEFAULT false,
    data jsonb NOT NULL CHECK (jsonb_typeof(data) = 'object'),
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP
);
CREATE OR REPLACE VIEW supply_box.maps AS
SELECT m->>'Name' AS name, m AS data
FROM supply_box.configuration c CROSS JOIN LATERAL jsonb_array_elements(c.data->'Maps') m;
CREATE OR REPLACE VIEW supply_box.spawn_points AS
SELECT m.name AS map_name, (p->>'Id')::integer AS id,
       (p->>'X')::double precision AS x, (p->>'Y')::double precision AS y,
       (p->>'Z')::double precision AS z, p AS data
FROM supply_box.maps m CROSS JOIN LATERAL jsonb_array_elements(m.data->'Points') p;
CREATE OR REPLACE VIEW supply_box.box_types AS
SELECT b->>'Key' AS key, b AS data
FROM supply_box.configuration c CROSS JOIN LATERAL jsonb_array_elements(c.data->'BoxTypes') b;
CREATE OR REPLACE VIEW supply_box.loot AS
SELECT b.key AS box_key, r AS data
FROM supply_box.box_types b CROSS JOIN LATERAL jsonb_array_elements(b.data->'Loot') r;

CREATE TABLE IF NOT EXISTS supply_box.radar_images (
    id varchar(64) PRIMARY KEY CHECK (id ~ '^[a-f0-9]{64}$'),
    data bytea NOT NULL CHECK (octet_length(data) BETWEEN 1 AND 8388608),
    mime varchar(32) NOT NULL CHECK (mime IN ('image/png', 'image/jpeg', 'image/webp')),
    file_name varchar(255) NOT NULL,
    width integer NOT NULL CHECK (width BETWEEN 64 AND 8192),
    height integer NOT NULL CHECK (height = width),
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP
);
