from typing import Generator
import pytest
from sqlalchemy import create_engine
from sqlalchemy.orm import sessionmaker, Session
from sqlalchemy.exc import IntegrityError
from data_context import (
    Base,
    User,
    Language,
    Phrase,
    Dictionary,
    Translation,
    Bookmark,
    TagCategory,
    Tag,
    BookmarkTag,
    Sync,
    ConflictLog,
)


# Fixture for in-memory DB and session
@pytest.fixture(scope="function")
def session() -> Generator[Session, None, None]:
    engine = create_engine("sqlite:///:memory:")
    Base.metadata.create_all(engine)
    SessionLocal = sessionmaker(bind=engine)
    db = SessionLocal()
    yield db
    db.close()


def test_create_user(session: Session) -> None:
    user = User(UserName="alice", CreatedAt=20250101)
    session.add(user)
    session.commit()
    assert session.query(User).count() == 1


def test_unique_phrase_constraint(session: Session) -> None:
    user = User(UserName="bob", CreatedAt=20250101)
    lang = Language(LanguageCode="EN", LanguageName="English")
    session.add_all([user, lang])
    session.commit()

    phrase1 = Phrase(
        PhraseId="p1",
        PhraseText="hello",
        PhraseLanguage="EN",
        Note="greeting",
        UserName="bob",
        ModifiedAt=20250102,
        DeleteFlag=False,
    )
    phrase2 = Phrase(
        PhraseId="p2",
        PhraseText="hello",
        PhraseLanguage="EN",
        Note="again",
        UserName="bob",
        ModifiedAt=20250103,
        DeleteFlag=False,
    )

    session.add(phrase1)
    session.commit()

    session.add(phrase2)
    with pytest.raises(IntegrityError):
        session.commit()


def test_relationship_cascade_phrase(session: Session) -> None:
    user = User(UserName="charlie", CreatedAt=20250101)
    lang = Language(LanguageCode="FR", LanguageName="French")
    session.add_all([user, lang])
    session.commit()

    phrase = Phrase(
        PhraseId="p1",
        PhraseText="bonjour",
        PhraseLanguage="FR",
        Note=None,
        UserName="charlie",
        ModifiedAt=20250101,
        DeleteFlag=False,
    )
    session.add(phrase)
    session.commit()
    assert session.query(Phrase).count() == 1

    session.delete(user)
    session.commit()
    assert session.query(Phrase).count() == 0


def test_dictionary_translation_bookmark(session: Session) -> None:
    lang1 = Language(LanguageCode="DE", LanguageName="German")
    lang2 = Language(LanguageCode="ES", LanguageName="Spanish")
    user = User(UserName="dan", CreatedAt=20250101)
    session.add_all([lang1, lang2, user])
    session.commit()

    dict_word = Dictionary(
        Word="haus",
        SourceLanguage="DE",
        Pronounce="haus",
        ModifiedAt=20250101,
        DeleteFlag=False,
    )
    session.add(dict_word)
    session.commit()

    translation = Translation(
        WordId=dict_word.WordId,
        TargetLanguage="ES",
        TranslationText="casa",
        ModifiedAt=20250102,
        DeleteFlag=False,
    )
    session.add(translation)
    session.commit()

    bookmark = Bookmark(
        BookmarkId="b1",
        UserName="dan",
        WordId=dict_word.WordId,
        Note="Important",
        ModifiedAt=20250102,
        DeleteFlag=False,
    )
    session.add(bookmark)
    session.commit()

    assert session.query(Translation).count() == 1
    assert session.query(Bookmark).count() == 1

    session.delete(lang1)
    session.commit()

    assert session.query(Dictionary).count() == 0
    assert session.query(Translation).count() == 0
    assert session.query(Bookmark).count() == 0


def test_tag_category_tag_bookmark_tag(session: Session) -> None:
    lang = Language(LanguageCode="DE", LanguageName="German")
    user = User(UserName="eva", CreatedAt=20250101)
    session.add_all([lang, user])
    session.commit()

    category = TagCategory(
        TagCategoryId="cat1",
        CategoryName="Priority",
        UserName="eva",
        Description="Urgent stuff",
        ModifiedAt=20250101,
        DeleteFlag=False,
    )
    tag = Tag(
        TagId="tag1",
        TagName="Urgent",
        TagCategoryId="cat1",
        Description="High Priority",
        ModifiedAt=20250101,
        DeleteFlag=False,
    )
    dict_word = Dictionary(
        Word="zeit",
        SourceLanguage="DE",
        Pronounce="tsait",
        ModifiedAt=20250101,
        DeleteFlag=False,
    )
    session.add_all([category, tag, dict_word])
    session.commit()

    bookmark = Bookmark(
        BookmarkId="bm1",
        UserName="eva",
        WordId=dict_word.WordId,
        Note="time word",
        ModifiedAt=20250101,
        DeleteFlag=False,
    )
    session.add(bookmark)
    session.commit()

    bookmark_tag = BookmarkTag(
        BookmarkTagId="bt1",
        BookmarkId="bm1",
        TagId="tag1",
        ModifiedAt=20250101,
        DeleteFlag=False,
    )
    session.add(bookmark_tag)
    session.commit()

    assert session.query(BookmarkTag).count() == 1

    session.delete(category)
    session.commit()
    assert session.query(Tag).count() == 0
    assert session.query(BookmarkTag).count() == 0


def test_sync_and_conflict_log(session: Session) -> None:
    user = User(UserName="fred", CreatedAt=20250101)
    session.add(user)
    session.commit()

    sync = Sync(UserName="fred", ClientId="client1", LastPull=20250101, LastPush=20250101)
    log = ConflictLog(
        ConflictLogId="c1",
        UserName="fred",
        TargetId="bookmark-id",
        ClientId="client1",
        Detail="Conflict in sync",
        ReportedAt=20250101,
    )
    session.add_all([sync, log])
    session.commit()

    assert session.query(Sync).count() == 1
    assert session.query(ConflictLog).count() == 1

    session.delete(user)
    session.commit()

    assert session.query(Sync).count() == 0
    assert session.query(ConflictLog).count() == 0
