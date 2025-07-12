from datetime import datetime, timezone
from typing import Any
import uuid
from sqlalchemy import create_engine, insert
from sqlalchemy.orm import Session
from data_context import Tag, TagCategory, User
from helpers import datetime_to_ticks
import en_zh_dict

# === Configuration ===
DB_PATH = "bear_words.db"
DB_URL = f"sqlite:///{DB_PATH}"

engine = create_engine(DB_URL, echo=True)
DT_NOW = datetime_to_ticks(datetime.now(timezone.utc))

USER_TO_CREATE = ["admin"]


# === Main ===
with Session(engine) as session:
    # Create users
    users: list[dict[str, str | int]] = []
    for user in USER_TO_CREATE:
        users.append({"UserName": user, "CreatedAt": DT_NOW})

    session.execute(insert(User), users)
    session.commit()

    # Prepare tag data
    tags = en_zh_dict.get_word_roots(en_zh_dict.ROOTS_PATH)

    # Add root tags for users
    for user in USER_TO_CREATE:
        tag_cat_id = str(uuid.uuid4())
        session.add(
            TagCategory(
                TagCategoryId=tag_cat_id,
                CategoryName="en wordroots",
                UserName=user,
                ModifiedAt=DT_NOW,
                DeleteFlag=False,
            )
        )
        session.commit()

        tag_data: list[dict[str, Any]] = []
        for k, v in tags.items():
            tag_data.append(
                {
                    "TagId": str(uuid.uuid4()),
                    "TagName": k,
                    "TagCategoryId": tag_cat_id,
                    "Description": v,
                    "ModifiedAt": DT_NOW,
                    "DeleteFlag": False,
                }
            )

        session.execute(insert(Tag), tag_data)
        session.commit()
