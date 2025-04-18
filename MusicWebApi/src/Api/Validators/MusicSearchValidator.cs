using FluentValidation;
using MusicWebApi.src.Api.Dto;

namespace MusicWebApi.src.Api.Validators;

public class MusicSearchValidator : AbstractValidator<MusicSearch>
{
    
    public MusicSearchValidator()
    {
        var searchValues = Enum.GetValues(typeof(MusicSearchOptions)) as int[]
            ?? throw new ArgumentException("Parameter cannot be empty", nameof(MusicSearchOptions));

        (int min, int max) searchOptionMaximums = (searchValues[0], searchValues[^1]);

        RuleFor(searchBody => searchBody.search)
            .NotEmpty();
        RuleFor(searchBody => searchBody.SearchOption)
            .Must(searchOption => 
                (int)searchOption >= searchOptionMaximums.min && 
                (int)searchOption <= searchOptionMaximums.max
            )
            .WithMessage("SearchOption must be within the valid range.");
    }
}
