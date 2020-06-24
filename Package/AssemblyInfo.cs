using System.Runtime.CompilerServices;

// Reveal internal classes like BitBuffer to test assembly
// When renaming the test assembly don't miss to change this param too!!
[assembly: InternalsVisibleTo("Tests")]
