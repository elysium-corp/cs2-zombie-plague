CREATE SCHEMA IF NOT EXISTS zombie_plague;
CREATE TABLE IF NOT EXISTS zombie_plague.class_catalog_settings (
    id integer PRIMARY KEY CHECK (id = 1),
    version bigint NOT NULL DEFAULT 0 CHECK (version >= 0),
    exported_version bigint NOT NULL DEFAULT 0 CHECK (exported_version >= 0),
    default_class varchar(64) NOT NULL,
    nemesis_class varchar(64) NOT NULL,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP
);
CREATE TABLE IF NOT EXISTS zombie_plague.zombie_classes (
    internal_name varchar(64) PRIMARY KEY,
    definition jsonb NOT NULL CHECK (jsonb_typeof(definition) = 'object')
);
CREATE TABLE IF NOT EXISTS zombie_plague.zombie_abilities (
    internal_name varchar(64) PRIMARY KEY,
    definition jsonb NOT NULL CHECK (jsonb_typeof(definition) = 'object')
);
CREATE TABLE IF NOT EXISTS zombie_plague.zombie_class_abilities (
    class_key varchar(64) NOT NULL REFERENCES zombie_plague.zombie_classes(internal_name) ON DELETE CASCADE,
    ability_key varchar(64) NOT NULL REFERENCES zombie_plague.zombie_abilities(internal_name) ON DELETE RESTRICT,
    sort_order integer NOT NULL CHECK (sort_order >= 0),
    PRIMARY KEY (class_key, ability_key),
    UNIQUE (class_key, sort_order)
);
CREATE TABLE IF NOT EXISTS zombie_plague.player_ability_assignments (
    steam_id bigint PRIMARY KEY CHECK (steam_id BETWEEN 76561197960265728 AND 76561202255233023),
    display_name varchar(160) NOT NULL DEFAULT '',
    enabled boolean NOT NULL DEFAULT TRUE
);
CREATE TABLE IF NOT EXISTS zombie_plague.player_abilities (
    steam_id bigint NOT NULL REFERENCES zombie_plague.player_ability_assignments(steam_id) ON DELETE CASCADE,
    ability_key varchar(64) NOT NULL REFERENCES zombie_plague.zombie_abilities(internal_name) ON DELETE RESTRICT,
    sort_order integer NOT NULL CHECK (sort_order >= 0),
    PRIMARY KEY (steam_id, ability_key),
    UNIQUE (steam_id, sort_order)
);
