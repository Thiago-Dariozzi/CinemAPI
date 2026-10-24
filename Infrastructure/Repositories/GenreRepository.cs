using Domain.Entities;
using Domain.Interfaces;

namespace Infrastructure.Repositories;

public sealed class GenreRepository(CinemAPIContext context) : BaseRepository<Genre>(context), IGenreRepository;
