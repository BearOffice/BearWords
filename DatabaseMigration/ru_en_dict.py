import csv

ADJ_PATH = "data/redict/adjectives.csv"
NOUNS_PATH = "data/redict/nouns.csv"
VERBS_PATH = "data/redict/verbs.csv"
OTHERS_PATH = "data/redict/others.csv"

ADJ_FIELDS = [
    ("comparative", "comparative form (e.g., 'bigger')"),
    ("superlative", "superlative form (e.g., 'biggest')"),
    ("short_m", "short form for masculine gender"),
    ("short_f", "short form for feminine gender"),
    ("short_n", "short form for neuter gender"),
    ("short_pl", "short form for plural"),
    ("decl_m_nom", "masculine nominative form"),
    ("decl_m_gen", "masculine genitive form"),
    ("decl_m_dat", "masculine dative form"),
    ("decl_m_acc", "masculine accusative form"),
    ("decl_m_inst", "masculine instrumental form"),
    ("decl_m_prep", "masculine prepositional form"),
    ("decl_f_nom", "feminine nominative form"),
    ("decl_f_gen", "feminine genitive form"),
    ("decl_f_dat", "feminine dative form"),
    ("decl_f_acc", "feminine accusative form"),
    ("decl_f_inst", "feminine instrumental form"),
    ("decl_f_prep", "feminine prepositional form"),
    ("decl_n_nom", "neuter nominative form"),
    ("decl_n_gen", "neuter genitive form"),
    ("decl_n_dat", "neuter dative form"),
    ("decl_n_acc", "neuter accusative form"),
    ("decl_n_inst", "neuter instrumental form"),
    ("decl_n_prep", "neuter prepositional form"),
    ("decl_pl_nom", "plural nominative form"),
    ("decl_pl_gen", "plural genitive form"),
    ("decl_pl_dat", "plural dative form"),
    ("decl_pl_acc", "plural accusative form"),
    ("decl_pl_inst", "plural instrumental form"),
    ("decl_pl_prep", "plural prepositional form"),
]


NOUNS_FIELDS = [
    ("gender", "grammatical gender (masculine, feminine, neuter)"),
    ("partner", "partner word (e.g., for aspect pairs)"),
    ("animate", "whether the noun is animate"),
    ("indeclinable", "whether the noun does not decline"),
    ("sg_only", "exists only in singular"),
    ("pl_only", "exists only in plural"),
    ("sg_nom", "singular nominative form"),
    ("sg_gen", "singular genitive form"),
    ("sg_dat", "singular dative form"),
    ("sg_acc", "singular accusative form"),
    ("sg_inst", "singular instrumental form"),
    ("sg_prep", "singular prepositional form"),
    ("pl_nom", "plural nominative form"),
    ("pl_gen", "plural genitive form"),
    ("pl_dat", "plural dative form"),
    ("pl_acc", "plural accusative form"),
    ("pl_inst", "plural instrumental form"),
    ("pl_prep", "plural prepositional form"),
]


VERBS_FIELDS = [
    ("aspect", "verb aspect (imperfective/perfective)"),
    ("partner", "aspectual partner verb"),
    ("imperative_sg", "imperative form (singular)"),
    ("imperative_pl", "imperative form (plural)"),
    ("past_m", "past tense, masculine"),
    ("past_f", "past tense, feminine"),
    ("past_n", "past tense, neuter"),
    ("past_pl", "past tense, plural"),
    ("presfut_sg1", "present/future, 1st person singular"),
    ("presfut_sg2", "present/future, 2nd person singular"),
    ("presfut_sg3", "present/future, 3rd person singular"),
    ("presfut_pl1", "present/future, 1st person plural"),
    ("presfut_pl2", "present/future, 2nd person plural"),
    ("presfut_pl3", "present/future, 3rd person plural"),
]

OTHERS_FIELDS: list[tuple[str, str]] = []


def get_word_prons_and_details(
    path: str, fields: list[tuple[str, str]]
) -> tuple[dict[str, str], dict[str, str]]:
    word_pron: dict[str, str] = {}
    word_trans: dict[str, str] = {}

    with open(path, newline="", encoding="utf-8") as f:
        reader = csv.DictReader(f, delimiter="\t")
        for row in reader:
            word = row["bare"].strip()
            accented = row["accented"].strip()
            trans = row["translations_en"].strip()

            if type(word) is not str:
                continue
            
            if type(accented) is str: 
                word_pron[word] = accented

            detail: str = ""
            if type(trans) is str:
                detail = trans

            lines: list[str] = []
            for field, field_desc in fields:
                value = row[field].strip()
                if type(value) is str and value != '':
                    lines.append(f"[{field_desc}] {value}")

            fields_str = "\n".join(lines)
            if len(lines) > 0:
                if detail == "":
                    detail = fields_str
                else:
                    detail += "\n-----\n" + fields_str

            word_trans[word] = detail

    return (word_pron, word_trans)
