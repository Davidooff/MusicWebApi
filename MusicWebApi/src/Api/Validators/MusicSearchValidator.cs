using FluentValidation;
using MusicWebApi.src.Api.Dto;
using MusicWebApi.src.Domain.Entities;

namespace MusicWebApi.src.Api.Validators;

public class MusicSearchValidator : AbstractValidator<MusicSearch>
{
    
    public MusicSearchValidator()
    {
        var searchValues = Enum.GetValues(typeof(EPlatform)) as int[]
            ?? throw new ArgumentException("Parameter cannot be empty", nameof(EPlatform));

        (int min, int max) searchOptionMaximums = (searchValues[0], searchValues[^1]);

        RuleFor(searchBody => searchBody.Search)
            .NotEmpty();
        RuleFor(searchBody => searchBody.Platform)
            .Must(searchOption => 
                (int)searchOption >= searchOptionMaximums.min && 
                (int)searchOption <= searchOptionMaximums.max
            )
            .WithMessage("SearchOption must be within the valid range.");
    }
}
