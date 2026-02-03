namespace fs_front.Services
{
    public class FormResponse
    {
        public bool Succeeded { get; set; }
        public string[] Errors { get; set; } = [];
    }
}