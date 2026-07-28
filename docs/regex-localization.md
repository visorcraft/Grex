---
title: Regex Builder Localization
layout: default
---

# Regex Builder Localization

The Regex Builder and visual breakdown use the same `.resw` catalogs as the rest of Grex. English source strings live in `Strings/en-US/Resources.resw`; every other culture has a matching catalog under `Strings/<culture>/Resources.resw`.

Relevant resource families include:

- Regex Builder control labels and presets;
- validation and sample-text messages;
- match-count messages;
- visual-breakdown type names;
- visual-breakdown descriptions for literals, groups, character classes, anchors, escapes, and quantifiers.

Do not maintain a copied key inventory here. The English catalog is authoritative and changes over time.

When changing Regex Builder text:

1. update the English resource;
2. propagate it with `Scripts/add_localization_entry.py` when the key is new;
3. preserve format placeholders;
4. mark affected translations incomplete until reviewed;
5. run the localization script tests;
6. run the Regex Builder localization tests on Windows.

See:

- [Translation and Localization](translations.md) for the full catalog workflow, status comments, scripts, and CRLF rules;
- [Feature Matrix](features.md#regex-builder) for user-visible Regex Builder behavior;
- [Build and Test](build-and-test.md) for Windows test commands.
