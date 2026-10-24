using Domain.Entities;
using Domain.Interfaces;

namespace Infrastructure.Repositories;

public sealed class ScreenRepository(CinemAPIContext context) : BaseRepository<Screen>(context), IScreenRepository;
