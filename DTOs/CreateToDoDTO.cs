using System;
using System.ComponentModel.DataAnnotations;

namespace TodoApi.DTOs
{
    public record CreateToDoDTO
    {
        [Required]
        [StringLength(255)]
        public string Description { get; init; }
        public DateTimeOffset Date { get; init; }
        public DateTimeOffset Time { get; init; }

        [StringLength(255)]
        public string Location { get; init; }
        
        [Range(1, 3)]
        public int Priority { get; init; }
        [Required]
        public Guid FkTagId { get; init; }

        [Required]
        public Guid FkUserId { get; init; }
    }
}