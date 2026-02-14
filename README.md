# PCTama


This app is made to be a PC Desktop Pet integrating AI to allow the pet to talk to the user.

Goals:
    Modeless app with transparency
    Using WinUI 3
    Need a controller that can associate various inputs with different models and the associated MCPs 

Initial Prompt: 
    Create a Cmake project that builds a product named PCTama that is a collection of ASP.NET controllers in an Aspire framework.
    The primary controller will be called "controller" and needs to use the official .NET MCP SDK to run a local LLM which connects to several local MCPs. 
    Two MCPs need to be developed: one called "text" to provide read-only input from a streaming text source and another called "actor" to provide actions that should be performed (example: say "hello world" ).
    Make sure to add configuration so that other MCPs for input data can be added.
    The text in the "text" MCP will be provided via OBS LocalVoice with other sources to be added later.
    The "actor" should use WinUI3 to display output to the user.
    Add a test suite to validate the controller, text, and actor. 
    Add a GitHub action workflow to both build the project and run the test suite.