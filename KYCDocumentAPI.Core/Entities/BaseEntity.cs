﻿using System.ComponentModel.DataAnnotations;

namespace KYCDocumentAPI.Core.Entities
{
    public abstract class BaseEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string? CreatedBy { get; set; }

        public string? UpdatedBy { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
