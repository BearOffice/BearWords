namespace BearWordsAPI.Shared.Helpers;

[AttributeUsage(AttributeTargets.Property)]
public class CascadeSoftDeleteAttribute : Attribute
{
    public bool OnDelete { get; set; } = true;
    public bool OnRestore { get; set; } = false;
}