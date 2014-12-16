### A C# library for composing your core business rules and freeing them from framework-specific validation.

[![Build Status](https://ci.appveyor.com/api/projects/status/github/jonsequitur/Its.Validation)](https://ci.appveyor.com/project/jonsequitur/its-validation)

Define a validation rule:

```csharp
var wontGoBadTooSoon = Validate.That<Fruit>(fruit => fruit.ExpirationDate > DateTime.Now.AddDays(5));
bool isValid = wontGoBadTooSoon.Check(lemon);
```

Compose it into other rules:

```csharp
var basketWontGoBadTooSoon = 
  Validate.That<FruitBasket>(basket =>
    basket.Fruits.Every(wontGoBadTooSoon));
```

Get information from the rule for display:

```csharp
var wontGoBadTooSoon = 
  Validate.That<Fruit>(fruit => 
    fruit.ExpirationDate.As("expiration") > DateTime.Now.AddDays(5));
```

Transform and format the information for display:

```
var wontGoBadTooSoon = 
  Validate.That<Fruit>(fruit =>
      fruit.As("fruitname", f => f.Name).ExpirationDate.As("expiration") > DateTime.Now.AddDays(5.As("days_in_transit")))
    .WithMessage("A {fruitname} that expires on {expiration:D} won't last for {days_in_transit} days.");

// OR localized:

    .WithMessage(Resources.BasketMustPreventScurvy);
```

Combine rules:

```csharp
var plan = new ValidationPlan<FruitBasket>
               {
                    basketHasFruit,
                    basketWontGoBadTooSoon
               };
```

Define rule dependencies:

```
var plan = new ValidationPlan<FruitBasket>
               {
                    basketHasFruit,               
                    basketWontGoBadTooSoon.When(basketHasFruit)
               };
```

Test your validation plan:

```csharp
var basket = new FruitBasket();

var failures = plan.Execute(basket);

failures.Count(f => f.Message == "Your basket must contain some fruit.")
        .Should()
        .Be(1));
```

Extend the validation results with your own types:

```csharp
var upcCodeExists = 
  Validation.That<Fruit>(fruit => database.UpcCodes.Exists(upc => upc.Code == fruit.UpcCode))
    .With<DatabaseError>(DatabaseErrors.UpcCodeDoesNotExist);
```
