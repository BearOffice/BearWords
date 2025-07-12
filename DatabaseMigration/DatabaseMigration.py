import json
import os
from datetime import datetime, timezone
from typing import Any
from sqlalchemy import create_engine, func, select, insert
from sqlalchemy.orm import Session
from data_context import Base, Dictionary, Language, Translation
from helpers import datetime_to_ticks, merge_without_overwrite
import ru_en_dict, en_zh_dict, ja_ja_dict

# === Configuration ===
DB_PATH = "bear_words.db"
DB_URL = f"sqlite:///{DB_PATH}"
EN_EN_PATH = "data/eedict/dictionary.json"
EN_PRON_PATH = "data/eedict/en_US.json"
EN_JA_PATH = "data/ejdict/ejdict.json"

# === Setup ===
if os.path.exists(DB_PATH):
    os.remove(DB_PATH)

engine = create_engine(DB_URL, echo=True)
Base.metadata.create_all(engine)
print("Blank database created.")

DT_NOW = datetime_to_ticks(datetime.now(timezone.utc))


# === Functions ===
def load_json(path: str) -> dict[str, str]:
    with open(path, "r", encoding="utf-8") as f:
        return json.load(f)


def get_max_word_id(session: Session) -> int:
    result = session.execute(select(func.max(Dictionary.WordId))).scalar()
    return result if result is not None else 0


def insert_words_and_translations(
    session: Session,
    word_trans_dict: dict[str, str],
    pron_dict: dict[str, str],
    source_lang: str,
    target_lang: str,
    word_id_acc: int,
    word_id_map: dict[str, int],
) -> int:
    new_words: list[dict[str, Any]] = []
    new_translations: list[dict[str, Any]] = []

    for word in sorted(word_trans_dict.keys()):
        if word not in word_id_map.keys():
            word_id_acc += 1
            new_words.append(
                {
                    "WordId": word_id_acc,
                    "Word": word,
                    "SourceLanguage": source_lang,
                    "Pronounce": pron_dict.get(word),
                    "ModifiedAt": DT_NOW,
                }
            )
            word_id_map[word] = word_id_acc
        new_translations.append(
            {
                "WordId": word_id_map[word],
                "TargetLanguage": target_lang,
                "TranslationText": word_trans_dict[word],
                "ModifiedAt": DT_NOW,
            }
        )

    session.execute(insert(Dictionary), new_words)
    session.execute(insert(Translation), new_translations)
    session.commit()
    return word_id_acc


# === Main Logic ===
with Session(engine) as session:
    # Insert supported languages
    session.add_all(
        [
            Language(LanguageCode="en", LanguageName="English"),
            Language(LanguageCode="ja", LanguageName="日本語"),
            Language(LanguageCode="ru", LanguageName="Русский"),
            Language(LanguageCode="zh-Hans", LanguageName="简体中文"),
            Language(LanguageCode="@none", LanguageName="None"),
        ]
    )
    session.commit()

    word_id_acc = get_max_word_id(session)
    word_id_map: dict[str, int] = {}

    # English-Chinese data preparation
    en_pron_alt, en_en_alt, en_zh = en_zh_dict.get_word_prons_and_details(
        en_zh_dict.WORDS_PATH, en_zh_dict.FIELDS
    )

    # English-English
    en_pron = load_json(EN_PRON_PATH)
    merge_without_overwrite(en_pron, en_pron_alt)

    en_en = load_json(EN_EN_PATH)
    merge_without_overwrite(en_en, en_en_alt)

    word_id_acc = insert_words_and_translations(
        session, en_en, en_pron, "en", "en", word_id_acc, word_id_map
    )

    # English-Japanese
    en_ja = load_json(EN_JA_PATH)
    word_id_acc = insert_words_and_translations(
        session, en_ja, en_pron, "en", "ja", word_id_acc, word_id_map
    )

    # # English-Chinese
    word_id_acc = insert_words_and_translations(
        session, en_zh, en_pron, "en", "zh-Hans", word_id_acc, word_id_map
    )

    # Japanese-Japanese
    ja_ja = ja_ja_dict.get_word_details(ja_ja_dict.WORDS_PATH)
    ja_pron = ja_ja_dict.get_word_prons(ja_ja_dict.PRONS_PATH)
    word_id_acc = insert_words_and_translations(
        session, ja_ja, ja_pron, "ja", "ja", word_id_acc, word_id_map
    )

    # Russian-English
    ru_pron: dict[str, str] = {}
    ru_en: dict[str, str] = {}

    for path, fields in [
        (ru_en_dict.ADJ_PATH, ru_en_dict.ADJ_FIELDS),
        (ru_en_dict.NOUNS_PATH, ru_en_dict.NOUNS_FIELDS),
        (ru_en_dict.VERBS_PATH, ru_en_dict.VERBS_FIELDS),
        (ru_en_dict.OTHERS_PATH, ru_en_dict.OTHERS_FIELDS),
    ]:
        p, e = ru_en_dict.get_word_prons_and_details(path, fields)
        ru_pron.update(p)
        ru_en.update(e)

    word_id_acc = insert_words_and_translations(
        session, ru_en, ru_pron, "ru", "en", word_id_acc, word_id_map
    )
