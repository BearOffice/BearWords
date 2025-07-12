from dataclasses import dataclass
import json
import re
from typing import Any

WORDS_PATH = "data/jjdict/jpn_wn_lmf_glosses_json_v2.txt"
PRONS_PATH = "data/jjdict/JmdictFurigana.json"
__PATTERN = r'^[A-Za-z!"#$%&\'()*+,\-./:;<=>?@[\\\]^_`{|}~ ]+$'


@dataclass
class ItemEntry:
    id: str
    item: str
    pos: str
    glosses: list[str]
    synonyms: list[str]
    synonyms2: list[list[str]]


def get_word_details(path: str) -> dict[str, str]:
    word_trans: dict[str, str] = {}

    with open(path, "r", encoding="utf-8") as f:
        next(f)  # skip credit line

        for line in f:
            data = json.loads(line)
            entry = ItemEntry(**data)

            # skip English words, symbols
            if re.fullmatch(__PATTERN, entry.item) is not None:
                continue

            if len(entry.glosses) == 1:
                detail = entry.glosses[0]
            else:
                lines: list[str] = []
                for i, g in enumerate(entry.glosses):
                    lines.append(f"[{i + 1}] {g}")
                detail = "\n".join(lines)

            lines: list[str] = []
            if entry.pos != "":
                lines.append(f"[品詞] {entry.pos}")
            if len(entry.synonyms2) == 1 and len(entry.synonyms2[0]) > 0:
                lines.append("[同義語・類義語] " + ", ".join(entry.synonyms))
            elif len(entry.synonyms2) > 1:
                syn_lines = ["[同義語・類義語]"]
                for i, s in enumerate(entry.synonyms2):
                    if len(s) > 0:
                        syn_lines.append(f"| [{i + 1}] " + ", ".join(s))

                if len(syn_lines) > 1:
                    lines.extend(syn_lines)

            fields_str = "\n".join(lines)
            if len(lines) > 0:
                if detail == "":
                    detail = fields_str
                else:
                    detail += "\n-----\n" + fields_str

            word_trans[entry.item] = detail

    return word_trans


def get_word_prons(path: str) -> dict[str, str]:
    word_prons: dict[str, str] = {}

    with open(path, "r", encoding="utf-8-sig") as f:
        data: list[dict[str, Any]] = json.load(f)

    for d in data:
        word_prons[d["text"]] = d["reading"]

    return word_prons
