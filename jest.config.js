module.exports = {
  moduleFileExtensions: ['js'],
  roots: ['./output'],
  testMatch: ['<rootDir>/**/*.Test.js'],
  coveragePathIgnorePatterns: ['/\.fable/', '/[fF]able.*/', '/node_modules/'],
  testEnvironment: 'node',
  transform: {}
};