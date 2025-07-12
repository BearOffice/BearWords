from typing import Optional, List, Any
from sqlalchemy import (
    Engine,
    ForeignKey,
    String,
    Integer,
    BigInteger,
    Boolean,
    UniqueConstraint,
    event,
)
from sqlalchemy.orm import (
    DeclarativeBase,
    relationship,
    Mapped,
    mapped_column,
)
from sqlalchemy.engine import Connection
from sqlite3 import Connection as SQLite3Connection


# https://stackoverflow.com/questions/5033547/sqlalchemy-cascade-delete
@event.listens_for(Engine, "connect")
def _set_sqlite_pragma(dbapi_connection: Any, connection_record: Connection) -> None:  # type: ignore
    if isinstance(dbapi_connection, SQLite3Connection):
        cursor = dbapi_connection.cursor()
        cursor.execute("PRAGMA foreign_keys=ON;")
        cursor.close()


class Base(DeclarativeBase):
    pass


class User(Base):
    __tablename__ = "User"

    UserName: Mapped[str] = mapped_column(String, primary_key=True)
    CreatedAt: Mapped[int] = mapped_column(BigInteger, nullable=False)

    phrases: Mapped[List["Phrase"]] = relationship(
        back_populates="user", passive_deletes="all"
    )
    bookmarks: Mapped[List["Bookmark"]] = relationship(
        back_populates="user", passive_deletes="all"
    )
    tag_categories: Mapped[List["TagCategory"]] = relationship(
        back_populates="user", passive_deletes="all"
    )
    syncs: Mapped[List["Sync"]] = relationship(
        back_populates="user", passive_deletes="all"
    )
    conflict_logs: Mapped[List["ConflictLog"]] = relationship(
        back_populates="user", passive_deletes="all"
    )


class Language(Base):
    __tablename__ = "Language"

    LanguageCode: Mapped[str] = mapped_column(String, primary_key=True)
    LanguageName: Mapped[str] = mapped_column(String, nullable=False)

    phrases: Mapped[List["Phrase"]] = relationship(
        back_populates="phrase_language", passive_deletes="all"
    )
    dictionaries: Mapped[List["Dictionary"]] = relationship(
        back_populates="source_language", passive_deletes="all"
    )
    translations_target: Mapped[List["Translation"]] = relationship(
        back_populates="target_language", passive_deletes="all"
    )


class Dictionary(Base):
    __tablename__ = "Dictionary"

    WordId: Mapped[int] = mapped_column(Integer, primary_key=True, autoincrement=True)
    Word: Mapped[str] = mapped_column(String, nullable=False)
    SourceLanguage: Mapped[str] = mapped_column(
        ForeignKey("Language.LanguageCode", ondelete="CASCADE")
    )
    Pronounce: Mapped[Optional[str]] = mapped_column(String)
    ModifiedAt: Mapped[int] = mapped_column(BigInteger, nullable=False)
    DeleteFlag: Mapped[bool] = mapped_column(Boolean, default=False, nullable=False)

    __table_args__ = (UniqueConstraint("Word", "SourceLanguage", name="uq_dictionary"),)

    source_language: Mapped["Language"] = relationship(
        back_populates="dictionaries", passive_deletes="all"
    )
    translations: Mapped[List["Translation"]] = relationship(
        back_populates="dictionary", passive_deletes="all"
    )
    bookmarks: Mapped[List["Bookmark"]] = relationship(
        back_populates="dictionary", passive_deletes="all"
    )


class Translation(Base):
    __tablename__ = "Translation"

    TranslationId: Mapped[int] = mapped_column(
        Integer, primary_key=True, autoincrement=True
    )
    WordId: Mapped[int] = mapped_column(
        ForeignKey("Dictionary.WordId", ondelete="CASCADE")
    )
    TargetLanguage: Mapped[str] = mapped_column(
        ForeignKey("Language.LanguageCode", ondelete="CASCADE")
    )
    TranslationText: Mapped[str] = mapped_column(String, nullable=False)
    ModifiedAt: Mapped[int] = mapped_column(BigInteger, nullable=False)
    DeleteFlag: Mapped[bool] = mapped_column(Boolean, default=False, nullable=False)

    __table_args__ = (
        UniqueConstraint("WordId", "TargetLanguage", name="uq_translation"),
    )

    dictionary: Mapped["Dictionary"] = relationship(
        back_populates="translations", passive_deletes="all"
    )
    target_language: Mapped["Language"] = relationship(
        back_populates="translations_target", passive_deletes="all"
    )


class Phrase(Base):
    __tablename__ = "Phrase"

    PhraseId: Mapped[str] = mapped_column(String, primary_key=True)
    PhraseText: Mapped[str] = mapped_column(String, nullable=False)
    PhraseLanguage: Mapped[str] = mapped_column(
        ForeignKey("Language.LanguageCode", ondelete="CASCADE")
    )
    Note: Mapped[Optional[str]] = mapped_column(String)
    UserName: Mapped[str] = mapped_column(
        ForeignKey("User.UserName", ondelete="CASCADE")
    )
    ModifiedAt: Mapped[int] = mapped_column(BigInteger, nullable=False)
    DeleteFlag: Mapped[bool] = mapped_column(Boolean, default=False, nullable=False)

    __table_args__ = (
        UniqueConstraint("PhraseText", "PhraseLanguage", "UserName", name="uq_phrase"),
    )

    phrase_language: Mapped["Language"] = relationship(
        back_populates="phrases", passive_deletes="all"
    )
    user: Mapped["User"] = relationship(back_populates="phrases", passive_deletes="all")
    phrase_tags: Mapped[List["PhraseTag"]] = relationship(
        back_populates="phrase", passive_deletes="all"
    )


class Bookmark(Base):
    __tablename__ = "Bookmark"

    BookmarkId: Mapped[str] = mapped_column(String, primary_key=True)
    UserName: Mapped[str] = mapped_column(
        ForeignKey("User.UserName", ondelete="CASCADE")
    )
    WordId: Mapped[int] = mapped_column(
        ForeignKey("Dictionary.WordId", ondelete="CASCADE")
    )
    Note: Mapped[Optional[str]] = mapped_column(String)
    ModifiedAt: Mapped[int] = mapped_column(BigInteger, nullable=False)
    DeleteFlag: Mapped[bool] = mapped_column(Boolean, default=False, nullable=False)

    __table_args__ = (UniqueConstraint("UserName", "WordId", name="uq_bookmark"),)

    user: Mapped["User"] = relationship(
        back_populates="bookmarks", passive_deletes="all"
    )
    dictionary: Mapped["Dictionary"] = relationship(
        back_populates="bookmarks", passive_deletes="all"
    )
    tags: Mapped[List["BookmarkTag"]] = relationship(
        back_populates="bookmark", passive_deletes="all"
    )


class TagCategory(Base):
    __tablename__ = "TagCategory"

    TagCategoryId: Mapped[str] = mapped_column(String, primary_key=True)
    CategoryName: Mapped[str] = mapped_column(String, nullable=False)
    UserName: Mapped[str] = mapped_column(
        ForeignKey("User.UserName", ondelete="CASCADE")
    )
    Description: Mapped[Optional[str]] = mapped_column(String)
    ModifiedAt: Mapped[int] = mapped_column(BigInteger, nullable=False)
    DeleteFlag: Mapped[bool] = mapped_column(Boolean, default=False, nullable=False)

    __table_args__ = (
        UniqueConstraint("CategoryName", "UserName", name="uq_tagcategory"),
    )

    user: Mapped["User"] = relationship(
        back_populates="tag_categories", passive_deletes="all"
    )
    tags: Mapped[List["Tag"]] = relationship(
        back_populates="tag_category", passive_deletes="all"
    )


class Tag(Base):
    __tablename__ = "Tag"

    TagId: Mapped[str] = mapped_column(String, primary_key=True)
    TagName: Mapped[str] = mapped_column(String, nullable=False)
    TagCategoryId: Mapped[str] = mapped_column(
        ForeignKey("TagCategory.TagCategoryId", ondelete="CASCADE")
    )
    Description: Mapped[Optional[str]] = mapped_column(String)
    ModifiedAt: Mapped[int] = mapped_column(BigInteger, nullable=False)
    DeleteFlag: Mapped[bool] = mapped_column(Boolean, default=False, nullable=False)

    __table_args__ = (UniqueConstraint("TagName", "TagCategoryId", name="uq_tag"),)

    tag_category: Mapped["TagCategory"] = relationship(
        back_populates="tags", passive_deletes="all"
    )
    bookmark_tags: Mapped[List["BookmarkTag"]] = relationship(
        back_populates="tag", passive_deletes="all"
    )
    phrase_tags: Mapped[List["PhraseTag"]] = relationship(
        back_populates="tag", passive_deletes="all"
    )


class BookmarkTag(Base):
    __tablename__ = "BookmarkTag"

    BookmarkTagId: Mapped[str] = mapped_column(String, primary_key=True)
    BookmarkId: Mapped[str] = mapped_column(
        ForeignKey("Bookmark.BookmarkId", ondelete="CASCADE")
    )
    TagId: Mapped[str] = mapped_column(ForeignKey("Tag.TagId", ondelete="CASCADE"))
    ModifiedAt: Mapped[int] = mapped_column(BigInteger, nullable=False)
    DeleteFlag: Mapped[bool] = mapped_column(Boolean, default=False, nullable=False)

    __table_args__ = (UniqueConstraint("BookmarkId", "TagId", name="uq_bookmark_tag"),)

    bookmark: Mapped["Bookmark"] = relationship(
        back_populates="tags", passive_deletes="all"
    )
    tag: Mapped["Tag"] = relationship(
        back_populates="bookmark_tags", passive_deletes="all"
    )


class PhraseTag(Base):
    __tablename__ = "PhraseTag"

    PhraseTagId: Mapped[str] = mapped_column(String, primary_key=True)
    PhraseId: Mapped[str] = mapped_column(
        ForeignKey("Phrase.PhraseId", ondelete="CASCADE")
    )
    TagId: Mapped[str] = mapped_column(ForeignKey("Tag.TagId", ondelete="CASCADE"))
    ModifiedAt: Mapped[int] = mapped_column(BigInteger, nullable=False)
    DeleteFlag: Mapped[bool] = mapped_column(Boolean, default=False, nullable=False)

    __table_args__ = (UniqueConstraint("PhraseId", "TagId", name="uq_phrase_tag"),)

    phrase: Mapped["Phrase"] = relationship(
        back_populates="phrase_tags", passive_deletes="all"
    )
    tag: Mapped["Tag"] = relationship(
        back_populates="phrase_tags", passive_deletes="all"
    )


class Sync(Base):
    __tablename__ = "Sync"

    UserName: Mapped[str] = mapped_column(
        ForeignKey("User.UserName", ondelete="CASCADE"), primary_key=True
    )
    ClientId: Mapped[str] = mapped_column(String, primary_key=True)
    LastPull: Mapped[int] = mapped_column(BigInteger, nullable=False)
    LastPush: Mapped[int] = mapped_column(BigInteger, nullable=False)

    user: Mapped["User"] = relationship(back_populates="syncs", passive_deletes="all")


class ConflictLog(Base):
    __tablename__ = "ConflictLog"

    ConflictLogId: Mapped[str] = mapped_column(String, primary_key=True)
    UserName: Mapped[str] = mapped_column(
        ForeignKey("User.UserName", ondelete="CASCADE")
    )
    ClientId: Mapped[str] = mapped_column(String, nullable=False)
    TargetId: Mapped[str] = mapped_column(String, nullable=False)
    Detail: Mapped[Optional[str]] = mapped_column(String)
    ReportedAt: Mapped[int] = mapped_column(BigInteger, nullable=False)

    user: Mapped["User"] = relationship(
        back_populates="conflict_logs", passive_deletes="all"
    )
