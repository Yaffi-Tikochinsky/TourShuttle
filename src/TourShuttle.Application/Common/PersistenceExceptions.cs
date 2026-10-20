namespace TourShuttle.Application.Common;

/// <summary>
/// Another request changed the same row between our read and our save (optimistic concurrency).
/// Infrastructure throws it in place of EF Core's DbUpdateConcurrencyException,
/// so Application never has to reference EF Core.
/// </summary>
public class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(Exception innerException)
        : base("The data was changed by another request.", innerException)
    {
    }
}

/// <summary>
/// The save broke a unique index, for example two registrations with the same email at the same moment.
/// </summary>
public class UniqueConstraintException : Exception
{
    public UniqueConstraintException(Exception innerException)
        : base("A record with the same unique value already exists.", innerException)
    {
    }
}

/// <summary>
/// The save broke a foreign key, for example deleting a destination while another request
/// is adding a tour to it. The service checks first; this covers the moment between check and save.
/// </summary>
public class ForeignKeyConstraintException : Exception
{
    public ForeignKeyConstraintException(Exception innerException)
        : base("The record is referenced by other records.", innerException)
    {
    }
}
