using System;
using System.Collections.Generic;
using BearWordsAPI.Shared.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace BearWordsAPI.Shared.Data;

public partial class BearWordsContext : DbContext
{
    public BearWordsContext()
    {
    }

    public BearWordsContext(DbContextOptions options)
        : base(options)
    {
    }

    public virtual DbSet<Bookmark> Bookmarks { get; set; }
    public virtual DbSet<BookmarkTag> BookmarkTags { get; set; }
    public virtual DbSet<ConflictLog> ConflictLogs { get; set; }
    public virtual DbSet<Dictionary> Dictionaries { get; set; }
    public virtual DbSet<Language> Languages { get; set; }
    public virtual DbSet<Phrase> Phrases { get; set; }
    public virtual DbSet<PhraseTag> PhraseTags { get; set; }
    public virtual DbSet<Sync> Syncs { get; set; }
    public virtual DbSet<Tag> Tags { get; set; }
    public virtual DbSet<TagCategory> TagCategories { get; set; }
    public virtual DbSet<Translation> Translations { get; set; }
    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Bookmark>(entity =>
        {
            entity.ToTable("Bookmark");

            entity.HasIndex(e => new { e.UserName, e.WordId }, "IX_Bookmark_UserName_WordId").IsUnique();

            entity.Property(e => e.BookmarkId).HasColumnType("VARCHAR");
            entity.Property(e => e.DeleteFlag).HasColumnType("BOOLEAN");
            entity.Property(e => e.ModifiedAt).HasColumnType("BIGINT");
            entity.Property(e => e.Note).HasColumnType("VARCHAR");
            entity.Property(e => e.UserName).HasColumnType("VARCHAR");

            entity.HasOne(d => d.UserNameNavigation).WithMany(p => p.Bookmarks).HasForeignKey(d => d.UserName);

            entity.HasOne(d => d.Word).WithMany(p => p.Bookmarks).HasForeignKey(d => d.WordId);
        });

        modelBuilder.Entity<BookmarkTag>(entity =>
        {
            entity.ToTable("BookmarkTag");

            entity.HasIndex(e => new { e.BookmarkId, e.TagId }, "IX_BookmarkTag_BookmarkId_TagId").IsUnique();

            entity.Property(e => e.BookmarkTagId).HasColumnType("VARCHAR");
            entity.Property(e => e.BookmarkId).HasColumnType("VARCHAR");
            entity.Property(e => e.DeleteFlag).HasColumnType("BOOLEAN");
            entity.Property(e => e.ModifiedAt).HasColumnType("BIGINT");
            entity.Property(e => e.TagId).HasColumnType("VARCHAR");

            entity.HasOne(d => d.Bookmark).WithMany(p => p.BookmarkTags).HasForeignKey(d => d.BookmarkId);

            entity.HasOne(d => d.Tag).WithMany(p => p.BookmarkTags).HasForeignKey(d => d.TagId);
        });

        modelBuilder.Entity<ConflictLog>(entity =>
        {
            entity.ToTable("ConflictLog");

            entity.Property(e => e.ConflictLogId).HasColumnType("VARCHAR");
            entity.Property(e => e.ClientId).HasColumnType("VARCHAR");
            entity.Property(e => e.Detail).HasColumnType("VARCHAR");
            entity.Property(e => e.ReportedAt).HasColumnType("BIGINT");
            entity.Property(e => e.TargetId).HasColumnType("VARCHAR");
            entity.Property(e => e.UserName).HasColumnType("VARCHAR");

            entity.HasOne(d => d.UserNameNavigation).WithMany(p => p.ConflictLogs).HasForeignKey(d => d.UserName);
        });

        modelBuilder.Entity<Dictionary>(entity =>
        {
            entity.HasKey(e => e.WordId);

            entity.ToTable("Dictionary");

            entity.HasIndex(e => new { e.Word, e.SourceLanguage }, "IX_Dictionary_Word_SourceLanguage").IsUnique();

            entity.Property(e => e.WordId).ValueGeneratedNever();
            entity.Property(e => e.DeleteFlag).HasColumnType("BOOLEAN");
            entity.Property(e => e.ModifiedAt).HasColumnType("BIGINT");
            entity.Property(e => e.Pronounce).HasColumnType("VARCHAR");
            entity.Property(e => e.SourceLanguage).HasColumnType("VARCHAR");
            entity.Property(e => e.Word).HasColumnType("VARCHAR");

            entity.HasOne(d => d.SourceLanguageNavigation).WithMany(p => p.Dictionaries).HasForeignKey(d => d.SourceLanguage);
        });

        modelBuilder.Entity<Language>(entity =>
        {
            entity.HasKey(e => e.LanguageCode);

            entity.ToTable("Language");

            entity.Property(e => e.LanguageCode).HasColumnType("VARCHAR");
            entity.Property(e => e.LanguageName).HasColumnType("VARCHAR");
        });

        modelBuilder.Entity<Phrase>(entity =>
        {
            entity.ToTable("Phrase");

            entity.HasIndex(e => new { e.PhraseText, e.PhraseLanguage, e.UserName }, "IX_Phrase_PhraseText_PhraseLanguage_UserName").IsUnique();

            entity.Property(e => e.PhraseId).HasColumnType("VARCHAR");
            entity.Property(e => e.DeleteFlag).HasColumnType("BOOLEAN");
            entity.Property(e => e.ModifiedAt).HasColumnType("BIGINT");
            entity.Property(e => e.Note).HasColumnType("VARCHAR");
            entity.Property(e => e.PhraseLanguage).HasColumnType("VARCHAR");
            entity.Property(e => e.PhraseText).HasColumnType("VARCHAR");
            entity.Property(e => e.UserName).HasColumnType("VARCHAR");

            entity.HasOne(d => d.PhraseLanguageNavigation).WithMany(p => p.Phrases).HasForeignKey(d => d.PhraseLanguage);

            entity.HasOne(d => d.UserNameNavigation).WithMany(p => p.Phrases).HasForeignKey(d => d.UserName);
        });

        modelBuilder.Entity<PhraseTag>(entity =>
        {
            entity.ToTable("PhraseTag");

            entity.HasIndex(e => new { e.PhraseId, e.TagId }, "IX_PhraseTag_PhraseId_TagId").IsUnique();

            entity.Property(e => e.PhraseTagId).HasColumnType("VARCHAR");
            entity.Property(e => e.DeleteFlag).HasColumnType("BOOLEAN");
            entity.Property(e => e.ModifiedAt).HasColumnType("BIGINT");
            entity.Property(e => e.PhraseId).HasColumnType("VARCHAR");
            entity.Property(e => e.TagId).HasColumnType("VARCHAR");

            entity.HasOne(d => d.Phrase).WithMany(p => p.PhraseTags).HasForeignKey(d => d.PhraseId);

            entity.HasOne(d => d.Tag).WithMany(p => p.PhraseTags).HasForeignKey(d => d.TagId);
        });

        modelBuilder.Entity<Sync>(entity =>
        {
            entity.HasKey(e => new { e.UserName, e.ClientId });

            entity.ToTable("Sync");

            entity.Property(e => e.UserName).HasColumnType("VARCHAR");
            entity.Property(e => e.ClientId).HasColumnType("VARCHAR");
            entity.Property(e => e.LastPull).HasColumnType("BIGINT");
            entity.Property(e => e.LastPush).HasColumnType("BIGINT");

            entity.HasOne(d => d.UserNameNavigation).WithMany(p => p.Syncs).HasForeignKey(d => d.UserName);
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.ToTable("Tag");

            entity.HasIndex(e => new { e.TagName, e.TagCategoryId }, "IX_Tag_TagName_TagCategoryId").IsUnique();

            entity.Property(e => e.TagId).HasColumnType("VARCHAR");
            entity.Property(e => e.DeleteFlag).HasColumnType("BOOLEAN");
            entity.Property(e => e.Description).HasColumnType("VARCHAR");
            entity.Property(e => e.ModifiedAt).HasColumnType("BIGINT");
            entity.Property(e => e.TagCategoryId).HasColumnType("VARCHAR");
            entity.Property(e => e.TagName).HasColumnType("VARCHAR");

            entity.HasOne(d => d.TagCategory).WithMany(p => p.Tags).HasForeignKey(d => d.TagCategoryId);
        });

        modelBuilder.Entity<TagCategory>(entity =>
        {
            entity.ToTable("TagCategory");

            entity.HasIndex(e => new { e.CategoryName, e.UserName }, "IX_TagCategory_CategoryName_UserName").IsUnique();

            entity.Property(e => e.TagCategoryId).HasColumnType("VARCHAR");
            entity.Property(e => e.CategoryName).HasColumnType("VARCHAR");
            entity.Property(e => e.DeleteFlag).HasColumnType("BOOLEAN");
            entity.Property(e => e.Description).HasColumnType("VARCHAR");
            entity.Property(e => e.ModifiedAt).HasColumnType("BIGINT");
            entity.Property(e => e.UserName).HasColumnType("VARCHAR");

            entity.HasOne(d => d.UserNameNavigation).WithMany(p => p.TagCategories).HasForeignKey(d => d.UserName);
        });

        modelBuilder.Entity<Translation>(entity =>
        {
            entity.ToTable("Translation");

            entity.HasIndex(e => new { e.WordId, e.TargetLanguage }, "IX_Translation_WordId_TargetLanguage").IsUnique();

            entity.Property(e => e.TranslationId).ValueGeneratedNever();
            entity.Property(e => e.DeleteFlag).HasColumnType("BOOLEAN");
            entity.Property(e => e.ModifiedAt).HasColumnType("BIGINT");
            entity.Property(e => e.TargetLanguage).HasColumnType("VARCHAR");
            entity.Property(e => e.TranslationText).HasColumnType("VARCHAR");

            entity.HasOne(d => d.TargetLanguageNavigation).WithMany(p => p.Translations).HasForeignKey(d => d.TargetLanguage);

            entity.HasOne(d => d.Word).WithMany(p => p.Translations).HasForeignKey(d => d.WordId);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserName);

            entity.ToTable("User");

            entity.Property(e => e.UserName).HasColumnType("VARCHAR");
            entity.Property(e => e.CreatedAt).HasColumnType("BIGINT");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
