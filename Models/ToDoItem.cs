namespace Backend.Models
{
    public class ToDoItem
    {
        public int Id { get; set; }
        public string Task { get; set; }
        public bool isCompleted { get; set; }
        public DateTime CreatedAt { get; set; }

        //FK
        public int UserId { get; set; }

    }

}
