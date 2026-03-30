import type { TemplateTask } from '@/components/types/template';


export function extractVariables(tasks: TemplateTask[]): string[] {
  const variablePattern = /\{\{(\s*\w+\s*)\}\}/g;
  const variables = new Set<string>();
  
  tasks.forEach(task => {
    const nameMatches = task.name.matchAll(variablePattern);
    for (const match of nameMatches) {
      variables.add(match[1].trim());
    }
    
    if (task.prompt) {
      const promptMatches = task.prompt.matchAll(variablePattern);
      for (const match of promptMatches) {
        variables.add(match[1].trim());
      }
    }
  });
  
  return Array.from(variables).sort();
}