export interface TextInputBlockProps {
  name: string;
  label: string;
  type: 'email' | 'password' | 'text' | 'date' | 'number';
  note?: string | null;
  placeholder?: string;
  autofocus?: boolean;
  regex?: string | undefined;
  value: string;
  onChange: (newValue: string) => void;
}

export function TextInputBlock(props: TextInputBlockProps) {
  return (
    <div>
      <label className="block text-sm font-medium text-gray-700 mb-1">
        {props.label}
      </label>
      <input
        name={props.name}
        type={props.type}
        placeholder={props.placeholder}
        value={props.value}
        pattern={props.regex ?? '.*'}
        onChange={(e) => props.onChange(e.target.value)}
        autoFocus={props.autofocus}
        className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
      />
      {props.note && (
        <div className="text-xs text-gray-500 mt-1">{props.note}</div>
      )}
    </div>
  );
}