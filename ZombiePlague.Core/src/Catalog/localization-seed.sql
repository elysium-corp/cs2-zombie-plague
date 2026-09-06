WITH inserted AS (
    INSERT INTO localization.entries(key, description, is_critical)
    SELECT @key, 'Перенесено из каталога классов', FALSE
    WHERE NOT EXISTS (SELECT 1 FROM localization.entries WHERE lower(key) = lower(@key))
    ON CONFLICT (key) DO NOTHING RETURNING id
)
INSERT INTO localization.translations(entry_id, language_code, text)
SELECT inserted.id, languages.code, @text FROM inserted
JOIN localization.languages languages ON languages.code = 'ru'
ON CONFLICT (entry_id, language_code) DO NOTHING;
