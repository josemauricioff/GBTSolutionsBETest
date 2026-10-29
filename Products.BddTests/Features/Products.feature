Feature: Product inventory management
  As an API client
  I want to manage products and stock
  So that product inventory remains accurate

  Scenario: A product can be created and retrieved
    When I create a product named "Safety glasses" with price 25.50 and stock 12
    Then the response status should be 201
    And the product identifier should have 6 digits
    And the created product should have name "Safety glasses" and stock 12

  Scenario: Invalid product data is rejected
    When I create a product named "" with price 25.50 and stock 12
    Then the response status should be 400

  Scenario: A product can have zero stock and a positive fractional price
    When I create a product named "Empty inventory" with price 0.01 and stock 0
    Then the response status should be 201
    And the created product should have name "Empty inventory" and stock 0

  Scenario Outline: Invalid numeric product values are rejected
    When I create a product named "Invalid numeric product" with price <price> and stock <stock>
    Then the response status should be 400

    Examples:
      | price | stock |
      | 0.00  | 0     |
      | -0.01 | 0     |
      | 1.00  | -1    |

  Scenario: Available stock can be decremented
    Given the product "Optical lens" has stock 25
    When I decrement the product stock by 3
    Then the response status should be 200
    And the product stock should be 22

  Scenario: Stock cannot be decremented below zero
    Given the product "Optical lens" has stock 25
    When I decrement the product stock by 26
    Then the response status should be 409
    And the product stock should be 25

  Scenario: Stock can be increased
    Given the product "Optical lens" has stock 25
    When I add 5 units to the product stock
    Then the response status should be 200
    And the product stock should be 30

  Scenario: Products can be found by a partial name
    When I search for products named "lens"
    Then the response status should be 200
    And the search results should contain "Optical lens" with stock 25
    And the search results should contain "Lens cleaner" with stock 50

  Scenario: Products can be found within an inclusive stock range
    When I search for products with stock between 10 and 25
    Then the response status should be 200
    And the search results should contain "Optical lens" with stock 25
    And the search results should contain "Frame" with stock 10
    And the search results should not contain "Lens cleaner"

  Scenario: An existing product can be updated
    Given the product "Optical lens" has stock 25
    When I update the product name to "Updated lens" with price 20.00 and stock 7
    Then the response status should be 204
    And the created product should have name "Updated lens" and stock 7

  Scenario: An existing product can be deleted
    Given the product "Optical lens" has stock 25
    When I delete the product
    Then the response status should be 204
    And the product should no longer exist

  Scenario: Two API instances generate unique identifiers concurrently
    When two API instances create 30 products concurrently
    Then all created products should have unique 6-digit identifiers

  Scenario: Concurrent stock decrements cannot oversell
    Given the product "Optical lens" has stock 25
    When two API instances each try to decrement stock by 20 concurrently
    Then one request should succeed and one should report insufficient stock
    And the product stock should be 5

  Scenario: Business validation rejects a whitespace-only name
    When I create a product named "   " with price 25.50 and stock 12
    Then the response status should be 400

  Scenario: Zero stock movement is rejected
    Given the product "Optical lens" has stock 25
    When I decrement the product stock by 0
    Then the response status should be 400
    And the product stock should be 25

  Scenario: Invalid stock search bounds are rejected
    When I search for products with stock between 20 and 10
    Then the response status should be 400

  Scenario: A missing product returns not found
    When I retrieve product 999999
    Then the response status should be 404

  Scenario: Stock cannot exceed the integer limit
    Given the product "Optical lens" has stock 25
    When I update the product name to "Optical lens" with price 20.00 and stock 2147483647
    Then the response status should be 204
    When I add 1 units to the product stock
    Then the response status should be 409
    And the product stock should be 2147483647