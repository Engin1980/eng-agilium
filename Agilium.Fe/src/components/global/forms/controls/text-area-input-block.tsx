export interface TextAreaInputBlockProps {
  name: string;
  label: string;
  placeholder?: string;
  value: string;
  rows?: number;
  onChange: (newValue: string) => void;
}

export function TextAreaInputBlock(props: TextAreaInputBlockProps) {
  return (
    <div>
      <label className="block text-sm font-medium text-gray-700 mb-1">
        {props.label}
      </label>
      <textarea
        name={props.name}
        rows={props.rows ?? 4}
        placeholder={props.placeholder}
        value={props.value}
        onChange={(e) => props.onChange(e.target.value)}
        className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
      />
    </div>
  );
}