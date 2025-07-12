import csv
import json
from typing import Any

WORDS_PATH = "data/ecdict/ecdict.csv"
SAMPLE_PATH = "data/ecdict/ecdict.mini.csv"
ROOTS_PATH = "data/ecdict/wordroot.txt"

FIELDS = [
    ("collins", "Collins"),
    ("oxford", "Oxford"),
    ("bnc", "BNC"),
    ("frq", "COCA"),
]


def get_word_prons_and_details(
    path: str, fields: list[tuple[str, str]]
) -> tuple[dict[str, str], dict[str, str], dict[str, str]]:
    word_pron: dict[str, str] = {}
    word_def: dict[str, str] = {}
    word_trans: dict[str, str] = {}

    with open(path, newline="", encoding="utf-8") as f:
        reader = csv.DictReader(f, delimiter=",")
        for row in reader:
            word = row["word"].strip()
            pron = row["phonetic"].strip()
            w_def = row["definition"].strip()
            trans = row["translation"].strip()

            if type(word) is not str:
                continue

            if type(pron) is str and pron != "":
                word_pron[word] = f"/{pron}/"

            if type(w_def) is str and w_def != "":
                word_def[word] = w_def

            detail: str = ""
            if type(trans) is str:
                detail = trans.replace("\\n", "\n")

            lines: list[str] = []
            for field, field_desc in fields:
                value = row[field].strip()
                if type(value) is str and value != "" and value != "0":
                    lines.append(f"[{field_desc}] {value}")

            fields_str = "\n".join(lines)
            if len(lines) > 0:
                if detail == "":
                    detail = fields_str
                else:
                    detail += "\n-----\n" + fields_str

            word_trans[word] = detail

    return (word_pron, word_def, word_trans)


def get_word_roots(path: str) -> dict[str, str]:
    with open(path, newline="", encoding="utf-8") as f:
        data: dict[str, dict[str, Any]] = json.load(f)

    roots: dict[str, str] = {}

    for k, v in data.items():
        class_: str = v["class"]
        meaning: str = v["meaning"]
        example: list[str] = v["example"]
        origin: str = v["origin"]

        detail = meaning
        lines: list[str] = []

        if class_ != "":
            lines.append(f"[Class] {class_}")
        if origin != "":
            lines.append(f"[Origin] {origin}")
        if len(example) > 0:
            lines.append(f"[Examples] " + ", ".join(example))

        fields_str = "\n".join(lines)
        if len(lines) > 0:
            if detail == "":
                detail = fields_str
            else:
                detail += "\n-----\n" + fields_str

        roots[k] = detail

    return roots


def get_word_root_with_examples(path: str) -> dict[str, list[str]]:
    with open(path, encoding="utf-8", mode="r") as f:
        data: dict[str, dict[str, Any]] = json.load(f)

    roots: dict[str, list[str]] = {}

    for k, v in data.items():
        examples: list[str] = v.get("example", [])
        roots[k] = examples

    return roots
