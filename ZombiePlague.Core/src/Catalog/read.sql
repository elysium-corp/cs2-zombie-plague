SELECT settings.version, settings.exported_version, jsonb_build_object(
    'FormatVersion', COALESCE((to_jsonb(settings)->>'format_version')::integer, 1), 'DefaultClass', settings.default_class, 'NemesisClass', settings.nemesis_class,
    'DefaultHumanClass', COALESCE(to_jsonb(settings)->>'default_human_class', ''),
    'SurvivorClass', COALESCE(to_jsonb(settings)->>'survivor_class', ''),
    'Classes', COALESCE((SELECT jsonb_agg(c.definition || jsonb_build_object(
        'InternalName', c.internal_name, 'Abilities', COALESCE((
            SELECT jsonb_agg(link.ability_key ORDER BY link.sort_order)
            FROM zombie_plague.zombie_class_abilities link WHERE link.class_key = c.internal_name
        ), '[]'::jsonb)) ORDER BY c.internal_name) FROM zombie_plague.zombie_classes c), '[]'::jsonb),
    'Abilities', COALESCE((SELECT jsonb_agg(a.definition || jsonb_build_object('InternalName', a.internal_name)
        ORDER BY a.internal_name) FROM zombie_plague.zombie_abilities a), '[]'::jsonb),
    'PlayerAbilities', COALESCE((SELECT jsonb_agg(jsonb_build_object(
        'SteamId', p.steam_id::text, 'DisplayName', p.display_name, 'Enabled', p.enabled,
        'Abilities', COALESCE((SELECT jsonb_agg(link.ability_key ORDER BY link.sort_order)
            FROM zombie_plague.player_abilities link WHERE link.steam_id = p.steam_id), '[]'::jsonb)
    ) ORDER BY p.steam_id) FROM zombie_plague.player_ability_assignments p), '[]'::jsonb)
)::text AS document
FROM zombie_plague.class_catalog_settings settings WHERE settings.id = 1 AND settings.version > 0
